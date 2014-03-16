using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KMP.Networking
{
    public class NetworkMessage : IDisposable
    {
        /// <summary>
        /// Header size in bytes
        /// Type
        /// </summary>
        private const int HEADER_SIZE = 1;

        /// <summary>
        /// The extra amonut of bytes to expand the array by to delay the next expansion
        /// </summary>
        private const int EXPAND_EXTRA = 1024;
        private readonly Encoding STRING_ENCODING = Encoding.UTF8;

        public PacketType Type { get; private set; }
        public byte[] Data { get; private set; }
        private int cursor;
        public NetworkMessage(PacketType type) :this()
        {
            this.Type = type;
            this.Data = new byte[1024];
        }

        /// <summary>
        /// Auto expands the internal array to hold more data
        /// </summary>
        /// <param name="amount">Amount of extra bytes the array will need to hold</param>
        private void AutoExpandArray(int amount)
        {
            if (this.cursor + amount >= this.Data.Length)
            {
                var data = new byte[this.Data.Length + amount + EXPAND_EXTRA];
                Array.Copy(this.Data, 0, data, 0, this.Data.Length);
                this.Data = data;
            }
        }

        /// <summary>
        /// Culls excess space from the internal array
        /// Compact before transmitting or storing
        /// </summary>
        public void Compact()
        {
            byte[] newArray = new byte[cursor];
            Array.Copy(this.Data, newArray, newArray.Length);
            this.Data = newArray;
        }

        private NetworkMessage()
        {
            cursor = 0;
        }

        /// <summary>
        /// Construct NetworkMessage from build data
        /// </summary>
        /// <param name="data">Data to build from</param>
        /// <returns>NetworkMessage</returns>
        public static NetworkMessage BuildFromData(byte[] data)
        {
            var type = (PacketType)data[0];
            var newData = new byte[data.Length-HEADER_SIZE];
            Array.Copy(data, HEADER_SIZE, newData, 0, newData.Length);
            return new NetworkMessage() { Data = newData, Type = type, cursor = newData.Length };
        }

        /// <summary>
        /// Write bytes directly to internal array
        /// </summary>
        /// <param name="data">Data to write</param>
        public void WriteBytes(byte[] data)
        {
            AutoExpandArray(data.Length);
            Array.Copy(data, 0, this.Data, cursor, data.Length);
            cursor += data.Length;
        }

        /// <summary>
        /// Read bytes from internal array
        /// </summary>
        /// <param name="count">Amount of bytes to read</param>
        /// <returns>Byte array of count length</returns>
        public byte[] ReadBytes(int count)
        {
            byte[] array = new byte[count];
            Fill(array);
            return array;
        }

        /// <summary>
        /// Fills an array with bytes from the internal array
        /// </summary>
        /// <param name="data">Array to fill</param>
        public void Fill(byte[] data)
        {
            if (this.Data.Length - cursor < data.Length)
                throw new IOException(String.Format("Ran out of data filling byte array (length:{0}). Cursor: {1}. Input data: {2}", data.Length, cursor, Data.Length));
            else
            {
                Array.Copy(this.Data, cursor, data, 0, data.Length);
                cursor += data.Length;
            }
        }

        /// <summary>
        /// Write a byte to the internal array
        /// </summary>
        /// <param name="b">Byte to write</param>
        public void WriteByte(byte b)
        {
            WriteBytes(new byte[] { b });
        }

        /// <summary>
        /// Read a single byte
        /// </summary>
        /// <returns>a byte from the internal array</returns>
        public byte ReadByte()
        {
            if (cursor >= this.Data.Length)
                throw new IOException(String.Format("Ran out of data reading byte"));
            return this.Data[cursor++];
        }

        /// <summary>
        /// Write a short to the buffer
        /// </summary>
        /// <param name="s">16bit int to write</param>
        public void WriteShort(short s)
        {
            WriteBytes(BitConverter.GetBytes(s));
        }

        /// <summary>
        /// Read short from buffer
        /// </summary>
        /// <returns>int16</returns>
        public short ReadShort()
        {
            return BitConverter.ToInt16(ReadBytes(2), 0);
        }

        /// <summary>
        /// Write int to the buffer
        /// </summary>
        /// <param name="i">int32 to write</param>
        public void WriteInt(int i)
        {
            WriteBytes(BitConverter.GetBytes(i));
        }

        /// <summary>
        /// Read int from buffer
        /// </summary>
        /// <returns>int32</returns>
        public int ReadInt()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }

        /// <summary>
        /// Write long to buffer
        /// </summary>
        /// <param name="l">int64 to write</param>
        public void WriteLong(long l)
        {
            WriteBytes(BitConverter.GetBytes(l));
        }

        /// <summary>
        /// Read long from buffer
        /// </summary>
        /// <returns>int64</returns>
        public long ReadLong()
        {
            return BitConverter.ToInt64(ReadBytes(8), 0);
        }

        /// <summary>
        /// Write float to buffer
        /// </summary>
        /// <param name="f">single point floating value</param>
        public void WriteFloat(float f)
        {
            WriteBytes(BitConverter.GetBytes(f));
        }

        /// <summary>
        /// Read float from buffer
        /// </summary>
        /// <returns>single floating</returns>
        public float ReadFloat()
        {
            return BitConverter.ToSingle(ReadBytes(4), 0);
        }

        /// <summary>
        /// Write double to buffer
        /// </summary>
        /// <param name="d">double precision floating value</param>
        public void WriteDouble(double d)
        {
            WriteBytes(BitConverter.GetBytes(d));
        }

        /// <summary>
        /// Read double from buffer
        /// </summary>
        /// <returns>double floating</returns>
        public double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBytes(8), 0);
        }

        /// <summary>
        /// Writes a byte array to the buffer, also writes the length so the exact amount of bytes can be read next time
        /// </summary>
        /// <param name="data">Data to write</param>
        public void WriteByteArray(byte[] data)
        {
            WriteInt(data.Length);
            WriteBytes(data);
        }

        /// <summary>
        /// Reads the byte array from the buffer. Array is prefixed by int32 length
        /// </summary>
        /// <returns>byte array</returns>
        public byte[] ReadByteArray()
        {
            return ReadBytes(ReadInt());
        }

        /// <summary>
        /// Writes UTF8 String using WriteByteArray
        /// </summary>
        /// <param name="s">String</param>
        public void WriteString(String s)
        {
            WriteByteArray(STRING_ENCODING.GetBytes(s));
        }

        /// <summary>
        /// Read UTF8 String using ReadByteArray
        /// </summary>
        /// <returns>UTF8 String</returns>
        public String ReadString()
        {
            return STRING_ENCODING.GetString(ReadByteArray());
        }

        /// <summary>
        /// Write a boolean value
        /// </summary>
        /// <param name="b">Boolean value to write</param>
        public void WriteBoolean(Boolean b)
        {
            WriteByte((byte)(b ? 255 : 0));
        }

        /// <summary>
        /// Read a boolean value
        /// </summary>
        /// <returns>Boolean</returns>
        public Boolean ReadBoolean()
        {
            return ReadByte() != 0;
        }

        /// <summary>
        /// Writes a Guid, only writes a single byte if it's empty to converse bandwidth
        /// </summary>
        /// <param name="guid">Guid to write</param>
        public void WriteGuid(Guid guid)
        {
            WriteBoolean(guid == Guid.Empty);
            if(guid != Guid.Empty) WriteBytes(guid.ToByteArray());
        }

        /// <summary>
        /// Reads a Guid
        /// </summary>
        /// <returns>Guid</returns>
        public Guid ReadGuid()
        {
            try
            {
                if (ReadBoolean()) return Guid.Empty;
                return new Guid(ReadBytes(16));
            }
            catch (ArgumentException ex)
            {
                Log.Debug("NetworkMessage Error: ReadGuid - Invalid array: {0}", ex);
            }
            catch (Exception ex)
            {
                Log.Debug("NetworkMessage Error: ReadGuid: {0}", ex);
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Writes a transmittable object to the message
        /// </summary>
        /// <param name="transmittable">Object to write</param>
        public void WriteObject(ITransmittable transmittable)
        {
            transmittable.TransmitObject(this);
        }

        /// <summary>
        /// Reads a transmittable object from the message
        /// </summary>
        /// <typeparam name="T">Object Type</typeparam>
        /// <returns>Object instance of Object Type</returns>
        public T ReadObject<T>() where T:ITransmittable
        {
            var type = typeof(T);
            var instance = type.GetConstructor(System.Type.EmptyTypes).Invoke(null) as ITransmittable;
            if (instance != null)
            {
                instance.ReceiveObject(this);
            }
            else return default(T);
            return (T)instance;
        }

        /// <summary>
        /// Write an Enum to the Message
        /// </summary>
        /// <typeparam name="T">Type of Enum</typeparam>
        /// <param name="e">Enum value</param>
        public void WriteEnum<T>(T e) where T: struct, IConvertible
        {
            if(!typeof(T).IsEnum) throw new ArgumentException("Type for WriteEnum is not an Enum");
            var intValue = (int)Convert.ChangeType(e, e.GetTypeCode());
            WriteInt(intValue);
        }

        /// <summary>
        /// Read an Enum from the Message
        /// </summary>
        /// <typeparam name="T">Type of Enum</typeparam>
        /// <returns>Enum value</returns>
        public T ReadEnum<T>() where T : struct, IConvertible
        {
            var value = ReadInt();
            return (T)Enum.ToObject(typeof(T), value);
        }

        /// <summary>
        /// Write Vector3
        /// </summary>
        /// <param name="vector">Vector3</param>
        public void WriteVector(Vector3 vector)
        {
            WriteFloat(vector.x);
            WriteFloat(vector.y);
            WriteFloat(vector.z);
        }

        /// <summary>
        /// Write Vector3d
        /// </summary>
        /// <param name="vector">Vector3d</param>
        public void WriteVector(Vector3d vector)
        {
            WriteDouble(vector.x);
            WriteDouble(vector.y);
            WriteDouble(vector.z);
        }

        /// <summary>
        /// Write Vector2
        /// </summary>
        /// <param name="vector">Vector2</param>
        public void WriteVector(Vector2 vector)
        {
            WriteFloat(vector.x);
            WriteFloat(vector.y);
        }

        /// <summary>
        /// Write Vector2d
        /// </summary>
        /// <param name="vector">Vector2d</param>
        public void WriteVector(Vector2d vector)
        {
            WriteDouble(vector.x);
            WriteDouble(vector.y);
        }

        /// <summary>
        /// Write Vector4
        /// </summary>
        /// <param name="vector">Vector4</param>
        public void WriteVector(Vector4 vector)
        {
            WriteFloat(vector.x);
            WriteFloat(vector.y);
            WriteFloat(vector.z);
            WriteFloat(vector.w);
        }
        /// <summary>
        /// Write Vector4d
        /// </summary>
        /// <param name="vector">Vector4d</param>
        public void WriteVector(Vector4d vector)
        {
            WriteDouble(vector.x);
            WriteDouble(vector.y);
            WriteDouble(vector.z);
            WriteDouble(vector.w);            
        }

        /// <summary>
        /// Read Vector
        /// </summary>
        /// <returns>Vector2</returns>
        public Vector2 ReadVector2()
        {
            return new Vector2(
                ReadFloat(), ReadFloat());
        }


        /// <summary>
        /// Read Vector
        /// </summary>
        /// <returns>Vector2d</returns>
        public Vector2d ReadVector2d()
        {
            return new Vector2d(
                ReadDouble(), ReadDouble());
        }


        /// <summary>
        /// Read Vector
        /// </summary>
        /// <returns>Vector3</returns>
        public Vector3 ReadVector3()
        {
            return new Vector3(
                ReadFloat(), ReadFloat(), ReadFloat());
        }

        /// <summary>
        /// Read Vector
        /// </summary>
        /// <returns>Vector3d</returns>
        public Vector3d ReadVector3d()
        {
            return new Vector3d(
                ReadDouble(), ReadDouble(), ReadDouble());
        }

        /// <summary>
        /// Read Vector
        /// </summary>
        /// <returns>Vector4</returns>
        public Vector4 ReadVector4()
        {
            return new Vector4(
                ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }

        /// <summary>
        /// Read Vector
        /// </summary>
        /// <returns>Vector4d</returns>
        public Vector4d ReadVector4d()
        {
            return new Vector4d(
                ReadDouble(), ReadDouble(), ReadDouble(), ReadDouble());
        }

        /// <summary>
        /// Compacts the internal array and returns the result with the Packet Type attached
        /// </summary>
        /// <returns>Returns compacted array</returns>
        public byte[] GetPacket()
        {
            Compact();
            byte[] data = new byte[this.Data.Length + HEADER_SIZE];
            data[0] = (byte)Type;
            Array.Copy(this.Data, 0, data, HEADER_SIZE, this.Data.Length);
            return data;
        }

        public void Dispose()
        {
            this.Data = null;
        }

        private enum VectorType
        {
            Vector2,
            Vector2d,
            Vector3,
            Vector3d,
            Vector4,
            Vector4d
        }
    }



    public enum PacketType
    {
        Handshake   /* Client: Username, Version */
            /* Server: ResponseCode, Protocol, Version, ClientID, Mode */
    }
}
