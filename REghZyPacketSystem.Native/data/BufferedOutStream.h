#ifndef __IMP_BUFFOUTSTREAM
#define __IMP_BUFFOUTSTREAM

#include "OutputStream.h"
class BufferedOutStream : public OutputStream {
public:
    BufferedOutStream(OutputStream* out) {
        m_out = out;
        m_buffer = new uint8_t[128];
        m_bufsize = 128;
        m_windex = 0;
    }

    BufferedOutStream(OutputStream* out, int bufferSize) {
        m_out = out;
        m_buffer = new uint8_t[bufferSize];
        m_bufsize = bufferSize;
        m_windex = 0;
    }

    void write(uint8_t* ptr, uintptr_t offset, uint16_t count) override {
        if (count >= m_bufsize) {
            m_out->write(ptr, offset, count);
            return;
        }

        uintptr_t next = m_windex + count;
        if (next < m_windex) { // overflow detection
            flush();
        }
        else if (next > m_bufsize) {
            flush();
        }

        memcpy(m_buffer + m_windex, ptr + offset, count);
        m_windex += count;
    }

    void write(uint8_t* ptr, uintptr_t offset, uint16_t count, asio::error_code err) {
        if (count >= m_bufsize) {
            flush(err);
            m_out->write(ptr, offset, count, err);
            return;
        }

        uintptr_t next = m_windex + count;
        if (next < m_windex) { // overflow detection
            flush(err);
        }
        else if (next > m_bufsize) {
            flush(err);
        }

        memcpy(m_buffer + m_windex, ptr + offset, count);
        m_windex += count;
    }

    void flush() override {
        m_out->write(m_buffer, 0, m_bufsize);
        m_out->flush();
        m_windex = 0;
    }

    void flush(asio::error_code& err) override {
        m_out->write(m_buffer, 0, m_bufsize, err);
        if (!err) {
            m_out->flush(err);
        }
    }

    void close() override {
        flush();
        m_out->close();
    }

    void close(asio::error_code& err) override {
        flush(err);
        m_out->close(err);
    }

    // Gets the underlying stream that this buffered output stream writes to
    OutputStream* getStream() {
        return m_out;
    }

private:
    OutputStream* m_out;
    uint8_t* m_buffer;
    uintptr_t m_windex;
    uint16_t m_bufsize;
};

#endif // !__IMP_BUFFOUTSTREAM