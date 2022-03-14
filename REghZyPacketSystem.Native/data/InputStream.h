#ifndef __INPUTSTR_IMPL
#define __INPUTSTR_IMPL

#include <asio.hpp>
#include <cstdint>

class InputStream {
protected:
	InputStream() { }
public:
    virtual uint16_t read(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count) {
        return count;
    }

    virtual uint16_t read(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count, asio::error_code& err) {
        return count;
    }

	virtual void readFully(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count) {
		uint16_t n = 0;
		while (n < count) {
			n += read(ptr, ptr_offset + n, count - n);
		}
	}

	virtual void readFully(uint8_t* ptr, uintptr_t ptr_offset, uint16_t count, asio::error_code& err) {
		uint16_t n = 0;
		while (n < count) {
			n += read(ptr, ptr_offset + n, count - n, err);
			if (err) {
				return;
			}
		}
	}

	virtual void close() { }
	virtual void close(asio::error_code& err) { }
};

#endif // !__INPUTSTR_IMPL