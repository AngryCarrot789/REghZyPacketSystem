#ifndef __IMPL_PACKET
#define __IMPL_PACKET
#include <cstdint>
#include "../data/DataStream.h"

#define MAX_PAYLOAD_LEN 1017

#define PREAMBLE_SEQ1 0b01110010 // 'r'
#define PREAMBLE_SEQ2 0b01111010 // 'z'
#define PREAMBLE_SEQ3 0b00110010 // '2'
#define PREAMBLE_SEQ4 0b00110001 // '1'

#define PROTOCOL_SUC_MASK 0b00000011 // A bitmask used for checking if an error is actually a successful protocol header read indicator
#define PROTOCOL_ERR_MASK 0b00011100 // A bitmask used for checking if an error is caused by a protocol header read failure
enum PROTOCOL_ERRCODE : uint8_t {
    PROTOCOL_SUC_TTTT = 0b00000001, // All preamble sequences were correct
    PROTOCOL_SUC_FTTT = 0b00000010, // All but the first preamble sequences were correct
    PROTOCOL_SUC_FFTT = 0b00000011, // All but the first and second preamble sequences were correct
    PROTOCOL_ERR_TTTF = 0b00000100, // All but the final preamble sequences were correct
    PROTOCOL_ERR_TTF0 = 0b00001000, // The first 2 preambles were correct, but the next one wasn't
    PROTOCOL_ERR_TF00 = 0b00001100, // The first preamble was correct, but the next one wasn't
    PROTOCOL_ERR_FTTF = 0b00010000, // Missing first preamble, but the next 2 were correct, but the last was incorrect
    PROTOCOL_ERR_FTF0 = 0b00010100, // Missing first preamble, but the next was correct, but the next was incorrect
    PROTOCOL_ERR_FFTF = 0b00011000, // Missing first 2 preambles, but the next was correct, but the last was incorrect
    PROTOCOL_ERR_FFF0 = 0b00011100  // Missing the first 3 preambles; the entire preamble check failed
};

#define PACKET_ERRCODE_MASK 0b10000000 // A bitmask used for checking if an error is caused by the creation of a packet
enum PACKET_ERRCODE : uint8_t {
    CTOR_INST_SUCCESS = 0b00000000, // A bitmask indicating a successful packet creation
    CTOR_READ_SUCCESS = 0b00000000, // A bitmask indicating a successful packet creation and payload read
    PKT_WRITE_SUCCESS = 0b00000000, // A bitmask indicating a successful packet payload write
    MISSING_READ_IMPL = 0b00000001, // A bitmask indicating the packet read implementation was missing
    MISSING_SEND_IMPL = 0b00000010, // A bitmask indicating the packet write implementation was missing
    MISSING_PACKET_ID = 0b00100000, // A bitmask used for checking if an error is caused by an invalid/unknown packet ID
    INVALID_PACKET_SZ = 0b01000000, // A bitmask used for checking if an error is caused by an invalid payload size (too large)
};

#define UTIL_COMB_PKR_ERROR(err) (PACKET_ERRCODE_MASK + (err << 8))
#define UTIL_GET_READ_ERROR(code) ((code >> 8) & 0b11111111)

#define REGISTER_PACKET(id, ctor) Packet::registerPacket(id, []() { return (Packet*) ctor; });

class Packet;

typedef Packet* (*pkt_ctor)();

Packet* (*ctor_table[255])();

class Packet {
public:
    Packet() {

    }

public:
    virtual uint8_t getId() { return 0; }

    // Reads this packet's data from the input stream
    // This usually returns 0, indicating no error. Any value above 0 is an error
    virtual uint8_t readPayload(DataInputStream* in, uint16_t len) {
        return MISSING_READ_IMPL;
    }

    // Writes this packet's payload to the given output stream
    // This usually returns 0, indicating no error. Any value above 0 is an error
    virtual uint8_t writePayload(DataOutputStream* in) {
        return MISSING_SEND_IMPL;
    }

    virtual uint16_t getPayloadSize() {
        return 0;
    }

    // Creates an instance of a packet. err will return a non-zero value, 
    // indicating an error (resulting in nullptr being returned)
    static Packet* createInstance(uint8_t id, uint8_t& err) {
        pkt_ctor ctor = ctor_table[id];
        if (ctor == nullptr) {
            err = PACKET_ERRCODE::MISSING_PACKET_ID;
            return nullptr;
        }

        err = PACKET_ERRCODE::CTOR_INST_SUCCESS;
        return ctor();
    }

    static void registerPacket(uint8_t id, pkt_ctor ctor) {
        ctor_table[id] = ctor;
    }

    static uint8_t writePacket(DataOutputStream* out, Packet* packet) {
        out->writeByte(PREAMBLE_SEQ1);
        out->writeByte(PREAMBLE_SEQ2);
        out->writeByte(PREAMBLE_SEQ3);
        out->writeByte(PREAMBLE_SEQ4);
        out->writeByte(packet->getId());
        out->writeUShort(packet->getPayloadSize());
        uint8_t err = packet->writePayload(out);
        out->flush();
        return err;
    }

    static Packet* readPacket(DataInputStream* in, uint16_t& err) {
        err = readProtocolHeader(in);
        if (err & PROTOCOL_SUC_MASK) {
            uint8_t id = in->readByte();
            pkt_ctor ctor = ctor_table[id];
            if (ctor == nullptr) {
                err = PACKET_ERRCODE::MISSING_PACKET_ID;
                return nullptr;
            }

            uint16_t len = in->readUShort();
            if (len > MAX_PAYLOAD_LEN) {
                err = PACKET_ERRCODE::INVALID_PACKET_SZ;
                return nullptr;
            }

            Packet* packet = ctor();
            int8_t readErr = packet->readPayload(in, len);
            if (readErr) {
                err = UTIL_COMB_PKR_ERROR(readErr);
                return nullptr;
            }

            err = PACKET_ERRCODE::CTOR_READ_SUCCESS;
            return packet;
        }
        else {
            return nullptr;
        }
    }

private:
    static uint16_t readProtocolHeader(DataInputStream* in) {
        uint8_t seq1 = in->readByte();
        if (seq1 == PREAMBLE_SEQ1) {
            if (in->readByte() == PREAMBLE_SEQ2) {
                if (in->readByte() == PREAMBLE_SEQ3) {
                    if (in->readByte() == PREAMBLE_SEQ4) {
                        return PROTOCOL_ERRCODE::PROTOCOL_SUC_TTTT;
                    }
                    else {
                        return PROTOCOL_ERRCODE::PROTOCOL_ERR_TTTF;
                    }
                }
                else {
                    return PROTOCOL_ERRCODE::PROTOCOL_ERR_TTF0;
                }
            }
            else {
                return PROTOCOL_ERRCODE::PROTOCOL_ERR_TF00;
            }
        }
        else if (in->readByte() == PREAMBLE_SEQ2) {
            if (in->readByte() == PREAMBLE_SEQ3) {
                if (in->readByte() == PREAMBLE_SEQ4) {
                    return PROTOCOL_ERRCODE::PROTOCOL_SUC_FTTT;
                }
                else {
                    return PROTOCOL_ERRCODE::PROTOCOL_ERR_FTTF;
                }
            }
            else {
                return PROTOCOL_ERRCODE::PROTOCOL_ERR_FTF0;
            }
        }
        else if (in->readByte() == PREAMBLE_SEQ3) {
            if (in->readByte() == PREAMBLE_SEQ4) {
                return PROTOCOL_ERRCODE::PROTOCOL_SUC_FFTT;
            }
            else {
                return PROTOCOL_ERRCODE::PROTOCOL_ERR_FFTF;
            }
        }
        else {
            return PROTOCOL_ERRCODE::PROTOCOL_ERR_FFF0;
        }
    }
};

#undef UTIL_COMB_PKR_READERR
#undef UTIL_COMB_PKR_SUCCESS
#ifdef HIDE_PREAMBLE
#undef PROTOCOL_SUC_MASK
#undef PREAMBLE_SEQ1
#undef PREAMBLE_SEQ2
#undef PREAMBLE_SEQ3
#undef PREAMBLE_SEQ4
#endif // HIDE_PREAMBLE

#endif // !__IMPL_PACKET
