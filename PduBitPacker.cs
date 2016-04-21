using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanceChecker
{
	public class PduBitPacker
	{
		#region Fields

		private static readonly byte[] EncodeMask;
		private static readonly byte[] DecodeMask;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new instance of the BytePacker class.
		/// </summary>
		static PduBitPacker()
		{
			EncodeMask = new byte[] { 1, 3, 7, 15, 31, 63, 127 };
			DecodeMask = new byte[] { 128, 192, 224, 240, 248, 252, 254 };
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Packs an unpacked 7 bit array to an 8 bit packed array according to the GSM
		/// protocol.
		/// </summary>
		/// <param name="unpackedBytes">The byte array that should be packed.</param>
		/// <returns>The packed bytes array.</returns>
		public static byte[] PackBytes(byte[] unpackedBytes)
		{
			return PackBytes(unpackedBytes, false, ' ');
		}

		/// <summary>
		/// Packs an unpacked 7 bit array to an 8 bit packed array according to the GSM
		/// protocol.
		/// </summary>
		/// <param name="unpackedBytes">The byte array that should be packed.</param>
		/// <param name="replaceInvalidChars">Indicates if invalid characters should be replaced by a '?' character.</param>
		/// <returns>The packed bytes array.</returns>
		public static byte[] PackBytes(byte[] unpackedBytes, bool replaceInvalidChars)
		{
			return PackBytes(unpackedBytes, replaceInvalidChars, '?');
		}

		/// <summary>
		/// Packs an unpacked 7 bit array to an 8 bit packed array according to the GSM
		/// protocol.
		/// </summary>
		/// <param name="unpackedBytes">The byte array that should be packed.</param>
		/// <param name="replaceInvalidChars">Indicates if invalid characters should be replaced by the default character.</param>
		/// <param name="defaultChar">The character that replaces invalid characters.</param>
		/// <returns>The packed bytes array.</returns>
		public static byte[] PackBytes(byte[] unpackedBytes, bool replaceInvalidChars, char defaultChar)
		{
			var defaultByte = (byte)defaultChar;
			var shiftedBytes = new byte[unpackedBytes.Length - (unpackedBytes.Length / 8)];

			var shiftOffset = 0;
			var shiftIndex = 0;

			// Shift the unpacked bytes to the right according to the offset (position of the byte)
			foreach (var b in unpackedBytes)
			{
				var tmpByte = b;

				// Handle invalid characters (bytes out of range)
				if (tmpByte > 127)
				{
					if (!replaceInvalidChars)
					{
						// throw exception and exit the method
						throw new Exception("Invalid character detected: " + tmpByte.ToString("X2"));
					}
					else
					{
						tmpByte = defaultByte;
					}
				}

				// Perform the byte shifting
				if (shiftOffset == 7)
				{
					shiftOffset = 0;
				}
				else
				{
					shiftedBytes[shiftIndex] = (byte)(tmpByte >> shiftOffset);
					shiftOffset++;
					shiftIndex++;
				}
			}

			var moveOffset = 1;
			var moveIndex = 1;
			var packIndex = 0;
			var packedBytes = new byte[shiftedBytes.Length];

			// Move the bits to the appropriate byte (pack the bits)
			foreach (var b in unpackedBytes)
			{
				if (moveOffset == 8)
				{
					moveOffset = 1;
				}
				else
				{
					if (moveIndex != unpackedBytes.Length)
					{
						// Extract the bits to be moved
						var extractedBitsByte = (unpackedBytes[moveIndex] & EncodeMask[moveOffset - 1]);
						// Shift the extracted bits to the proper offset
						extractedBitsByte = (extractedBitsByte << (8 - moveOffset));
						// Move the bits to the appropriate byte (pack the bits)
						var movedBitsByte = (extractedBitsByte | shiftedBytes[packIndex]);

						packedBytes[packIndex] = (byte)movedBitsByte;

						moveOffset++;
						packIndex++;
					}
					else
					{
						packedBytes[packIndex] = shiftedBytes[packIndex];
					}
				}

				moveIndex++;
			}

			return packedBytes;
		}


		/// <summary>
		///  Unpacks a packed 8 bit array to a 7 bit unpacked array according to the GSM
		///  Protocol.
		/// </summary>
		/// <param name="packedBytes">The byte array that should be unpacked.</param>
		/// <returns>The unpacked bytes array.</returns>
		public static byte[] UnpackBytes(byte[] packedBytes)
		{
			var shiftedBytes = new byte[(packedBytes.Length * 8) / 7];

			var shiftOffset = 0;
			var shiftIndex = 0;

			// Shift the packed bytes to the left according to the offset (position of the byte)
			foreach (var b in packedBytes)
			{
				if (shiftOffset == 7)
				{
					shiftedBytes[shiftIndex] = 0;
					shiftOffset = 0;
					shiftIndex++;
				}

				shiftedBytes[shiftIndex] = (byte)((b << shiftOffset) & 127);

				shiftOffset++;
				shiftIndex++;
			}

			var moveOffset = 0;
			var moveIndex = 0;
			int[] unpackIndex = {1};
			var unpackedBytes = new byte[shiftedBytes.Length];

			// 
			if (shiftedBytes.Length > 0)
			{
				unpackedBytes[unpackIndex[0] - 1] = shiftedBytes[unpackIndex[0] - 1];
			}

			// Move the bits to the appropriate byte (unpack the bits)
			foreach (var b in packedBytes.Where(b => unpackIndex[0] != shiftedBytes.Length))
			{
			    if (moveOffset == 7)
			    {
			        moveOffset = 0;
			        unpackIndex[0]++;
			        unpackedBytes[unpackIndex[0] - 1] = shiftedBytes[unpackIndex[0] - 1];
			    }

			    if (unpackIndex[0] == shiftedBytes.Length) continue;
			    // Extract the bits to be moved
			    var extractedBitsByte = (packedBytes[moveIndex] & DecodeMask[moveOffset]);
			    // Shift the extracted bits to the proper offset
			    extractedBitsByte = (extractedBitsByte >> (7 - moveOffset));
			    // Move the bits to the appropriate byte (unpack the bits)
			    var movedBitsByte = (extractedBitsByte | shiftedBytes[unpackIndex[0]]);

			    unpackedBytes[unpackIndex[0]] = (byte)movedBitsByte;

			    moveOffset++;
			    unpackIndex[0]++;
			    moveIndex++;
			}

			// Remove the padding if exists
		    if (unpackedBytes[unpackedBytes.Length - 1] != 0) return unpackedBytes;
		    var finalResultBytes = new byte[unpackedBytes.Length - 1];
		    Array.Copy(unpackedBytes, 0, finalResultBytes, 0, finalResultBytes.Length);

		    return finalResultBytes;
		}


		/// <summary>
		/// Converts hex string into the equivalent byte array.
		/// </summary>
		/// <param name="hexString">The hex string to be converted.</param>
		/// <returns>The equivalent byte array.</returns>
		public static byte[] ConvertHexToBytes(string hexString)
		{
			if (hexString.Length % 2 != 0)
				return null;

			var len = hexString.Length / 2;
			var array = new byte[len];

			for (var i = 0; i < array.Length; i++)
			{
				var tmp = hexString.Substring(i * 2, 2);
				array[i] = byte.Parse(tmp, NumberStyles.HexNumber);
			}

			return array;
		}

		/// <summary>
		/// Converts a byte array into the equivalent hex string.
		/// </summary>
		/// <param name="byteArray">The byte array to be converted.</param>
		/// <returns>The equivalent hex string.</returns>
		public static string ConvertBytesToHex(byte[] byteArray)
		{
			if (byteArray == null)
				return "";

			var sb = new StringBuilder();
			foreach (var b in byteArray)
			{
				sb.Append(b.ToString("X2"));
			}

			return sb.ToString();
		}

		#endregion
	}
}
