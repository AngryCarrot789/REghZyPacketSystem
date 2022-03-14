#ifndef __IMPL_DATAINSTREAM
#define __IMPL_DATAINSTREAM

#include "InputStream.h"
#include <asio.hpp>

class DataInputStream {
public:
    DataInputStream(InputStream* in) {
        m_in = in;
    }

public:
    uint16_t read(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count) {
        return m_in->read(ptr, ptr_offset, count);
    }

    uint16_t read(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count, asio::error_code& err) {
        return m_in->read(ptr, ptr_offset, count, err);
    }

    void readFully(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count) {
        m_in->readFully(ptr, ptr_offset, count);
    }

    void readFully(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count, asio::error_code& err) {
        m_in->readFully(ptr, ptr_offset, count, err);
    }

    bool readBool() {
        uint8_t b;
        m_in->readFully(&b, 0, 1);
        return b;
    }

    int8_t readSByte() {
        uint8_t v;
        m_in->readFully(&v, 0, 1);
        return (int8_t)v;
    }

    uint8_t readByte() {
        uint8_t v;
        m_in->readFully(&v, 0, 1);
        return v;
    }

    int16_t readShort() {
        uint16_t v;
        m_in->readFully((uint8_t*)&v, 0, 2);
        return (int16_t)v;
    }

    uint16_t readUShort() {
        uint16_t v;
        m_in->readFully((uint8_t*)&v, 0, 2);
        return v;
    }

    int32_t readInt() {
        uint32_t v;
        m_in->readFully((uint8_t*)&v, 0, 4);
        return (int32_t)v;
    }

    uint32_t readUInt() {
        uint32_t v;
        m_in->readFully((uint8_t*)&v, 0, 4);
        return v;
    }

    int64_t readLong() {
        uint64_t v;
        m_in->readFully((uint8_t*)&v, 0, 8);
        return (int64_t)v;
    }

    uint64_t readULong() {
        uint64_t v;
        m_in->readFully((uint8_t*)&v, 0, 8);
        return v;
    }

    char readCharUTF8() {
        uint8_t v;
        m_in->readFully(&v, 0, 1);
        return (char)v;
    }

    wchar_t readCharUTF16() {
        uint8_t v[2];
        m_in->readFully(v, 0, 2);
        return (wchar_t)((uint16_t)(v[0] << 8) + v[1]);
    }
    
    void readStringUTF8(char* ptr, uint16_t length) {
        readStringUTF8(ptr, 0, length);
    }

    void readStringUTF16(wchar_t* ptr, uint16_t length) {
        return readStringUTF16(ptr, 0, length);
    }

    void readStringUTF8(char* ptr, uintptr_t ptr_offset, uint16_t length) {
        m_in->readFully((uint8_t*)ptr, ptr_offset, length);
    }

    void readStringUTF16(wchar_t* ptr, uintptr_t ptr_offset, uint16_t length) {
        uint8_t buf[8];
        while (length > 3) {
            m_in->readFully(buf, 0, 8);
            ptr[ptr_offset + 0] = (wchar_t)((uint16_t)(buf[0] << 8) + buf[1]);
            ptr[ptr_offset + 1] = (wchar_t)((uint16_t)(buf[2] << 8) + buf[3]);
            ptr[ptr_offset + 2] = (wchar_t)((uint16_t)(buf[4] << 8) + buf[5]);
            ptr[ptr_offset + 3] = (wchar_t)((uint16_t)(buf[6] << 8) + buf[7]);
            length -= 4;
            ptr_offset += 4;
        }

        if (length == 3) {
            m_in->readFully(buf, 0, 6);
            ptr[ptr_offset + 0] = (wchar_t)((uint16_t)(buf[0] << 8) + buf[1]);
            ptr[ptr_offset + 1] = (wchar_t)((uint16_t)(buf[2] << 8) + buf[3]);
            ptr[ptr_offset + 2] = (wchar_t)((uint16_t)(buf[4] << 8) + buf[5]);
            return;
        }
        else if (length == 2) {
            m_in->readFully(buf, 0, 4);
            ptr[ptr_offset + 0] = (wchar_t)((uint16_t)(buf[0] << 8) + buf[1]);
            ptr[ptr_offset + 1] = (wchar_t)((uint16_t)(buf[2] << 8) + buf[3]);
            return;
        }
        else if (length == 1) {
            m_in->readFully(buf, 0, 2);
            ptr[ptr_offset] = (wchar_t)((uint16_t)(buf[0] << 8) + buf[1]);
            return;
        }
    }

    InputStream* getStream() {
        return m_in;
    }

private:
    InputStream* m_in;
};

#endif // !__IMPL_DATAINSTREAM;