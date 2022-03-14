using System.IO;
using System.Runtime.CompilerServices;
using REghZy.Streams;

namespace REghZyPacketSystem.Packeting {
    /// <summary>
    /// A class for helping with getting the size of objects in bytes
    /// </summary>
    public static class PacketUtils {
        /// <summary>
        /// Gets the byte array's length with a label
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSizeWL(this byte[] value) {
            if (value == null || value.Length == 0) {
                return 2;
            }
            else {
                return 2 + value.Length;
            }
        }

        /// <summary>
        /// Gets the number of bytes in the string (including the length label size), based on UTF-16 (2 bytes per character)
        /// </summary>
        /// <returns>
        /// 2 bytes (length label), plus the length of the string multiplied by 2 (0 if the string is empty).
        /// This will never return below 2 (due to the length label)
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSizeUTF16WL(this string value) {
            if (value == null || value.Length == 0) {
                return 2;
            }
            else {
                return 2 + (value.Length * 2);
            }
        }

        /// <summary>
        /// Gets the number of bytes in the string (without a length label), based on UTF-16 (2 bytes per character)
        /// </summary>
        /// <returns>
        /// The length of the string multiplied by 2 (0 if the string is empty)
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSizeUTF16NL(this string value) {
            if (value == null || value.Length == 0) {
                return 0;
            }
            else {
                return value.Length * 2;
            }
        }

        /// <summary>
        /// Gets the number of bytes in the string (including the length label size), based on UTF-8 (1 byte per character)
        /// </summary>
        /// <returns>
        /// 2 bytes (length label), plus the length of the string (0 if the string is empty).
        /// This will never return below 2 (due to the length label)
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSizeUTF8WL(this string value) {
            if (value == null || value.Length == 0) {
                return 2;
            }
            else {
                return 2 + value.Length;
            }
        }

        /// <summary>
        /// Gets the number of bytes in the string (without a length label), based on UTF-8 (1 byte per character)
        /// </summary>
        /// <returns>
        /// The length of the string (0 if the string is empty)
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSizeUTF8NL(this string value) {
            if (value == null || value.Length == 0) {
                return 0;
            }
            else {
                return value.Length;
            }
        }

        /// <summary>
        /// Writes 2 bytes (a short, being the length of the string), and 2 bytes for each each character of the given char array
        /// </summary>
        /// <param name="value">The chars to write</param>
        /// <param name="output">The data output to write to</param>
        public static void WriteStringUTF16WL(this IDataOutput output, char[] value) {
            if (value == null || value.Length == 0) {
                output.WriteUShort(0);
            }
            else {
                output.WriteUShort((ushort) value.Length);
                output.WriteCharsUTF16(value);
            }
        }

        /// <summary>
        /// Writes 2 bytes (a short, being the length of the string), and 2 bytes for each character of the given character array
        /// <para>
        /// If the given string is null, it will simply write 2 bytes of value '0' (resulting in an empty string being received on the other side)
        /// </para>
        /// </summary>
        /// <param name="value">The string to write</param>
        /// <param name="output">The data output to write to</param>
        public static void WriteStringUTF16WL(this IDataOutput output, string value) {
            if (value == null || value.Length == 0) {
                output.WriteUShort(0);
            }
            else {
                output.WriteUShort((ushort) value.Length);
                output.WriteStringUTF16(value);
            }
        }

        /// <summary>
        /// Writes 2 bytes (a short, being the length of the string), and 1 byte (the low byte) of each character of the given char array
        /// </summary>
        /// <param name="value">The chars to write</param>
        /// <param name="output">The data output to write to</param>
        public static void WriteStringUTF8WL(this IDataOutput output, char[] value) {
            if (value == null || value.Length == 0) {
                output.WriteUShort(0);
            }
            else {
                output.WriteUShort((ushort) value.Length);
                output.WriteCharsUTF8(value);
            }
        }

        /// <summary>
        /// Writes 2 bytes (a short, being the length of the string), and 1 byte (the low byte) of each character of the given string
        /// <para>
        /// If the given string is null, it will simply write 2 bytes of value '0' (resulting in an empty string being received on the other side)
        /// </para>
        /// </summary>
        /// <param name="value">The string to write</param>
        /// <param name="output">The data output to write to</param>
        public static void WriteStringUTF8WL(this IDataOutput output, string value) {
            if (value == null || value.Length == 0) {
                output.WriteUShort(0);
            }
            else {
                output.WriteUShort((ushort) value.Length);
                output.WriteStringUTF8(value);
            }
        }

        /// <summary>
        /// Reads 2 bytes (being the length of a string) as a short value, and reads that many characters (2 bytes per character)
        /// </summary>
        /// <param name="input">The data input</param>
        /// <returns>A string</returns>
        public static string ReadStringUTF16WL(this IDataInput input) {
            ushort length = input.ReadUShort();
            if (length == 0) {
                return null;
            }

            try {
                return input.ReadStringUTF16(length);
            }
            catch (IOException e) {
                throw new IOException("Failed to read a UTF16 string (of length " + length + ")", e);
            }
        }

        /// <summary>
        /// Reads 2 bytes (being the length of a string) as a short value, and reads that many characters/bytes
        /// </summary>
        /// <param name="input">The data input</param>
        /// <returns>A string</returns>
        public static string ReadStringUTF8WL(this IDataInput input) {
            ushort length = input.ReadUShort();
            if (length == 0) {
                return null;
            }

            try {
                return input.ReadStringUTF8(length);
            }
            catch (IOException e) {
                throw new IOException("Failed to read a UTF8 string (of length " + length + ")", e);
            }
        }

        /// <summary>
        /// Reads 2 bytes (being the length of a string) as a short value, and reads that many bytes
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static void WriteBytesWL(this IDataOutput output, byte[] array) {
            if (array == null || array.Length == 0) {
                output.WriteUShort(0);
            }
            else {
                output.WriteUShort((ushort) array.Length);
                output.Write(array, 0, array.Length);
            }
        }

        /// <summary>
        /// Reads 2 bytes (being the length of a string) as a short value, and reads that many bytes
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] ReadBytesWL(this IDataInput input) {
            ushort length = input.ReadUShort();
            byte[] buffer = new byte[length];
            if (length > 0) {
                input.ReadFully(buffer, 0, length);
            }

            return buffer;
        }
    }
}