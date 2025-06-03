using System.Text;

namespace RespawnApi.Application.Utils
{
    /// <summary>
    /// Provides utility methods for parsing binary data from byte arrays.
    /// All methods expect the offset to be managed externally and passed by reference.
    /// </summary>
    public static class BinaryDataParser
    {
        /// <summary>
        /// Reads a null-terminated string from the buffer at the specified offset using the given encoding.
        /// Advances the offset past the string and its null terminator.
        /// </summary>
        /// <param name="buffer">The byte array to read from.</param>
        /// <param name="offset">The starting offset in the buffer. Will be advanced by this method.</param>
        /// <param name="encoding">The encoding to use for the string.</param>
        /// <param name="logger">Optional logger for warnings.</param>
        /// <returns>The parsed string.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the offset is outside the bounds of the buffer before or after reading.</exception>
        public static string ReadNullTerminatedString(byte[] buffer, ref int offset, Encoding encoding,
            ILogger? logger = null)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (offset < 0 || offset > buffer.Length) // Check if offset is within bounds
                throw new IndexOutOfRangeException(
                    $"Initial offset {offset} is out of bounds for buffer of length {buffer.Length}.");

            int start = offset;
            int end = offset;
            while (end < buffer.Length && buffer[end] != 0x00) // find null terminator
            {
                end++;
            }

            string result;
            if (end < buffer.Length) // Null terminator found
            {
                result = encoding.GetString(buffer, start, end - start);
                offset = end + 1; // Move past the null terminator
            }
            else // No null terminator found within buffer bounds
            {
                logger?.LogWarning(
                    "ReadNullTerminatedString: String not null-terminated or extends beyond buffer. Offset: {Offset}, BufferLength: {Length}. Reading to end.",
                    start, buffer.Length);
                result = encoding.GetString(buffer, start, buffer.Length - start);
                offset = buffer.Length; // Move offset to the end of the buffer
            }

            return result;
        }

        /// <summary>
        /// Reads a single byte from the buffer at the specified offset.
        /// Advances the offset by 1.
        /// </summary>
        /// <param name="buffer">The byte array to read from.</param>
        /// <param name="offset">The starting offset in the buffer. Will be advanced by this method.</param>
        /// <returns>The byte read.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the offset is outside the bounds of the buffer.</exception>
        public static byte ReadByte(byte[] buffer, ref int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length)
                throw new IndexOutOfRangeException(
                    $"Offset {offset} is out of bounds for buffer of length {buffer.Length}. Cannot read byte.");

            byte result = buffer[offset];
            offset++;
            return result;
        }

        /// <summary>
        /// Reads a 16-bit signed integer (short) from the buffer in little-endian format at the specified offset.
        /// Advances the offset by 2.
        /// </summary>
        /// <param name="buffer">The byte array to read from.</param>
        /// <param name="offset">The starting offset in the buffer. Will be advanced by this method.</param>
        /// <returns>The 16-bit signed integer read.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the offset is outside the bounds of the buffer or not enough bytes remain.</exception>
        /// not used
        public static short ReadInt16LittleEndian(byte[] buffer, ref int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset + 2 > buffer.Length)
            {
                throw new IndexOutOfRangeException(
                    $"Offset {offset} is out of bounds or not enough data for Int16 in buffer of length {buffer.Length}.");
            }

            short result = BitConverter.ToInt16(buffer, offset);
            offset += 2;
            return result;
        }

        /// <summary>
        /// Reads a 32-bit signed integer (int) from the buffer in little-endian format at the specified offset.
        /// Advances the offset by 4.
        /// </summary>
        /// <param name="buffer">The byte array to read from.</param>
        /// <param name="offset">The starting offset in the buffer. Will be advanced by this method.</param>
        /// <returns>The 32-bit signed integer read.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the offset is outside the bounds of the buffer or not enough bytes remain.</exception>
        public static int ReadInt32LittleEndian(byte[] buffer, ref int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset + 4 > buffer.Length)
            {
                throw new IndexOutOfRangeException(
                    $"Offset {offset} is out of bounds or not enough data for Int32 in buffer of length {buffer.Length}.");
            }

            int result = BitConverter.ToInt32(buffer, offset);
            offset += 4;
            return result;
        }

        /// <summary>
        /// Reads a single-precision floating-point number (float) from the buffer in little-endian format at the specified offset.
        /// Advances the offset by 4.
        /// </summary>
        /// <param name="buffer">The byte array to read from.</param>
        /// <param name="offset">The starting offset in the buffer. Will be advanced by this method.</param>
        /// <returns>The float read.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the offset is outside the bounds of the buffer or not enough bytes remain.</exception>
        public static float ReadFloatLittleEndian(byte[] buffer, ref int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset + 4 > buffer.Length)
            {
                throw new IndexOutOfRangeException(
                    $"Offset {offset} is out of bounds or not enough data for Float in buffer of length {buffer.Length}.");
            }

            float result = BitConverter.ToSingle(buffer, offset);
            offset += 4;
            return result;
        }
    }
}