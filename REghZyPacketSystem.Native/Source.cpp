#include <iostream>

#ifndef _WIN32
#define _WIN32_WINNT 0x0A00
#endif // !_WIN32

#define ASIO_STANDALONE
#include <asio.hpp>
#include <asio/ts/buffer.hpp>
#include <asio/ts/internet.hpp>
#include <asio/serial_port.hpp>

using namespace asio;

int main() {
	error_code err;
	io_context context;
	serial_port port(context);
	port.open("COM20", err);

	if (err) {
		std::cout << "Error: " << err.message() << '\n';
	}
	else {
		std::cout << "Connected to COM20!" << '\n';
	}

	port.close(err);
	if (err) {
		std::cout << "Error: " << err.message() << '\n';
	}
	else {
		std::cout << "Disconnected from COM20!" << '\n';
	}
}