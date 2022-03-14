#ifndef __IMPL_SERIALOUTSTREAM
#define __IMPL_SERIALOUTSTREAM

#include "../OutputStream.h"
#include <asio.hpp>
class SerialOutStream : public OutputStream {
public:
    SerialOutStream(asio::serial_port* port, asio::io_context* io) {
        m_port = port;
        m_io = io;
        m_portname = nullptr;
    }

    SerialOutStream(asio::serial_port* port, asio::io_context* io, const char* port_name) {
        m_port = port;
        m_io = io;
        m_portname = port_name;
    }

public:
    void write(uint8_t* ptr, uintptr_t offset, uint16_t count) override {
        m_port->write_some(asio::buffer(ptr + offset, count));
    }

    void write(uint8_t* ptr, uintptr_t offset, uint16_t count, asio::error_code& err) override {
        m_port->write_some(asio::buffer(ptr + offset, count), err);
    }

    void close() {
        m_port->close();
    }

    void close(asio::error_code& err) {
        m_port->close(err);
    }

    void flush() { }

    void flush(asio::error_code& err) { }

    asio::serial_port* getPort() {
        return m_port;
    }

    const char* getPortName() {
        return m_portname;
    }

    void dispose(bool closePort, bool port, bool io) {
        if (closePort) {
            m_port->close();
        }

        if (port) {
            delete(m_port);
        }
        if (io) {
            delete(m_io);
        }
    }

private:
    asio::serial_port* m_port;
    asio::io_context* m_io;
    const char* m_portname;
};

#endif // !__IMPL_SERIALOUTSTREAM