#ifndef __IMPL_SERIALINSTREAM
#define __IMPL_SERIALINSTREAM

#include "../InputStream.h"
#include <asio.hpp>
class SerialInStream : public InputStream {
public:
    SerialInStream(asio::serial_port* port, asio::io_context* io) {
        m_port = port;
        m_io = io;
        m_portname = nullptr;
    }

    SerialInStream(asio::serial_port* port, asio::io_context* io, const char* port_name) {
        m_port = port;
        m_io = io;
        m_portname = port_name;
    }

public:
    uint16_t read(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count) {
        return m_port->read_some(asio::buffer(ptr + ptr_offset, count));
    }

    uint16_t read(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count, asio::error_code& err) {
        return m_port->read_some(asio::buffer(ptr + ptr_offset, count), err);
    }

    void close() {
        m_port->close();
    }

    void close(asio::error_code& err) {
        m_port->close(err);
    }

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

#endif // !__IMPL_SERIALINSTREAM