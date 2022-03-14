#ifndef __IMPL_DATAOUTSTREAM
#define __IMPL_DATAOUTSTREAM

#include <asio.hpp>
#include "OutputStream.h"
class DataOutputStream {
public:
	DataOutputStream(OutputStream* out) {
		m_out = out;
		m_wbuf8 = new uint8_t[8];
	}

public:
    void flush() {
        m_out->flush();
    }

    void close() {
        m_out->close();
    }

	void write(uint8_t* ptr, uintptr_t offset, uint16_t count) {
		m_out->write(ptr, offset, count);
	}

	void writeBool(bool value) {
		m_out->write((uint8_t*)&value, 0, 1);
	}

	void writeSByte(int8_t i) {
		m_out->write((uint8_t*)&i, 0, 1);
	}

	void writeByte(uint8_t i) {
		m_out->write(&i, 0, 1);
	}

	void writeShort(int16_t i) {
		writeUShort((uint16_t)i);
	}

	void writeUShort(uint16_t i) {
		m_wbuf8[0] = (i >> 8) & 255;
		m_wbuf8[1] = (i >> 0) & 255;
		m_out->write(m_wbuf8, 0, 2);
	}

	void writeInt(int32_t i) {
		writeUInt((uint32_t)i);
	}

	void writeUInt(uint32_t i) {
		m_wbuf8[0] = (i >> 24) & 255;
		m_wbuf8[1] = (i >> 16) & 255;
		m_wbuf8[2] = (i >> 8) & 255;
		m_wbuf8[3] = (i >> 0) & 255;
		m_out->write(m_wbuf8, 0, 4);
	}

	void writeLong(int64_t i) {
		writeULong((uint64_t)i);
	}

	void writeULong(uint64_t i) {
		m_wbuf8[0] = (i >> 56) & 255;
		m_wbuf8[1] = (i >> 48) & 255;
		m_wbuf8[2] = (i >> 40) & 255;
		m_wbuf8[3] = (i >> 32) & 255;
		m_wbuf8[4] = (i >> 24) & 255;
		m_wbuf8[5] = (i >> 16) & 255;
		m_wbuf8[6] = (i >> 8) & 255;
		m_wbuf8[7] = (i >> 0) & 255;
		m_out->write(m_wbuf8, 0, 8);
	}

	void writeFloat(float_t i) {
		uint32_t bits = *(uint32_t*)&i;
		m_wbuf8[0] = (bits >> 24) & 255;
		m_wbuf8[1] = (bits >> 16) & 255;
		m_wbuf8[2] = (bits >> 8) & 255;
		m_wbuf8[3] = (bits >> 0) & 255;
		m_out->write(m_wbuf8, 0, 4);
	}

	void writeDouble(double_t i) {
		uint64_t bits = *(uint64_t*)&i;
		m_wbuf8[0] = (bits >> 56) & 255;
		m_wbuf8[1] = (bits >> 48) & 255;
		m_wbuf8[2] = (bits >> 40) & 255;
		m_wbuf8[3] = (bits >> 32) & 255;
		m_wbuf8[4] = (bits >> 24) & 255;
		m_wbuf8[5] = (bits >> 16) & 255;
		m_wbuf8[6] = (bits >> 8) & 255;
		m_wbuf8[7] = (bits >> 0) & 255;
		m_out->write(m_wbuf8, 0, 8);
	}

	void writeCharUTF8(char ch) {
		m_out->write((uint8_t*)&ch, 0, 1);
	}

	void writeCharUTF16(wchar_t ch) {
		m_wbuf8[0] = ((uint16_t)ch >> 8) & 255;
		m_wbuf8[1] = ((uint16_t)ch >> 0) & 255;
		m_out->write(m_wbuf8, 0, 2);
	}

    void writeCharUTF32(wchar_t ch) {
        uint32_t c = (uint32_t)ch;
        m_wbuf8[0] = (c >> 32) & 255;
        m_wbuf8[1] = (c >> 16) & 255;
        m_wbuf8[2] = (c >> 8) & 255;
        m_wbuf8[3] = (c >> 0) & 255;
        m_out->write(m_wbuf8, 0, 4);
    }

	void writeStringUTF8(const char* str, uintptr_t offset, uint16_t length) {
		m_out->write((uint8_t*)str, offset, length);
	}

    void writeStringUTF16(const wchar_t* str, uintptr_t offset, uint16_t length) {
        int len = length;
        int off = offset;
        uint8_t* ptr = (uint8_t*)str;
        while (len > 3) {
            m_wbuf8[0] = (ptr[off + 0] >> 8) & 255;
            m_wbuf8[1] = (ptr[off + 0] >> 0) & 255;
            m_wbuf8[2] = (ptr[off + 1] >> 8) & 255;
            m_wbuf8[3] = (ptr[off + 1] >> 0) & 255;
            m_wbuf8[4] = (ptr[off + 2] >> 8) & 255;
            m_wbuf8[5] = (ptr[off + 2] >> 0) & 255;
            m_wbuf8[6] = (ptr[off + 3] >> 8) & 255;
            m_wbuf8[7] = (ptr[off + 3] >> 0) & 255;
            m_out->write(m_wbuf8, 0, 8);
            off += 8;
            len -= 8;
        }

        if (len == 3) {
            m_wbuf8[0] = (ptr[off + 0] >> 8) & 255;
            m_wbuf8[1] = (ptr[off + 0] >> 0) & 255;
            m_wbuf8[2] = (ptr[off + 1] >> 8) & 255;
            m_wbuf8[3] = (ptr[off + 1] >> 0) & 255;
            m_wbuf8[4] = (ptr[off + 2] >> 8) & 255;
            m_wbuf8[5] = (ptr[off + 2] >> 0) & 255;
            m_out->write(m_wbuf8, 0, 6);
            return;
        }
        else if (len == 2) {
            m_wbuf8[0] = (ptr[off + 0] >> 8) & 255;
            m_wbuf8[1] = (ptr[off + 0] >> 0) & 255;
            m_wbuf8[2] = (ptr[off + 1] >> 8) & 255;
            m_wbuf8[3] = (ptr[off + 1] >> 0) & 255;
            m_out->write(m_wbuf8, 0, 4);
            return;
        }
        else if (len == 1) {
            m_wbuf8[0] = (ptr[off] >> 8) & 255;
            m_wbuf8[1] = (ptr[off] >> 0) & 255;
            m_out->write(m_wbuf8, 0, 2);
            return;
        }
        else {
            return;
        }
    }

	void writePtr(uint8_t* ptr, int offset, int len) {
		m_out->write(ptr, offset, len);
	}

    OutputStream* getOut() {
        return m_out;
    }

private:
	OutputStream* m_out;
	uint8_t* m_wbuf8;
};

#endif // !__IMPL_DATAOUTSTREAM;