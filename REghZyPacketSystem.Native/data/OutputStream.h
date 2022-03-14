#ifndef __OUTPUTSTR_IMPL
#define __OUTPUTSTR_IMPL

#include <asio.hpp>
#include <cstdint>

class OutputStream {
protected:
	OutputStream() { }
public:
	virtual void write(uint8_t* ptr, uintptr_t offset, uint16_t count) { }
	virtual void write(uint8_t* ptr, uintptr_t offset, uint16_t count, asio::error_code& err) { }

	virtual void close() { }
	virtual void close(asio::error_code& err) { }

	virtual void flush() { }
	virtual void flush(asio::error_code& err) { }
};

#endif // !__OUTPUTSTR_IMPL