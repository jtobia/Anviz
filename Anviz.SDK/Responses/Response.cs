﻿using Anviz.SDK.Utils;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Anviz.SDK.Responses
{
    public class Response
    {
        enum RetVal
        {
            SUCCESS = 0x00, // operation successful
            FAIL = 0x01, // operation failed
            FULL = 0x04, // user full
            EMPTY = 0x05, // user empty
            NO_USER = 0x06, // user not exist
            TIME_OUT = 0x08, // capture timeout
            USER_OCCUPIED = 0x0A, // user already exists
            FINGER_OCCUPIED = 0x0B, // fingerprint already exists
        }

        public ulong DeviceID { get; }

        public byte[] DATA { get; }

        internal Response(byte[] data, ulong deviceId)
        {
            DATA = data;
            DeviceID = deviceId;
        }

        internal static async Task<Response> FromStream(byte ResponseCode, NetworkStream stream)
        {
            var base_offset = 6;
            var data = new byte[1500];
            if (await stream.ReadAsync(data, 0, base_offset) != base_offset)
            {
                throw new Exception("Partial packet read");
            }

            var STX = data[0];
            if (STX != 0xA5)
            {
                throw new Exception("Invalid header");
            }
            var CH = Bytes.Split(data, 1, 4);
            var ACK = data[5];
            if (ACK < 0x80)
            {
                if (await stream.ReadAsync(data, base_offset, 2) != 2)
                {
                    throw new Exception("Partial packet read");
                }
                base_offset += 2;
            }
            else
            {
                if (ACK != ResponseCode)
                {
                    throw new Exception("Invalid ACK");
                }
                if (await stream.ReadAsync(data, base_offset, 3) != 3)
                {
                    throw new Exception("Partial packet read");
                }
                var RET = (RetVal)data[6];
                if (RET != RetVal.SUCCESS)
                {
                    throw new Exception("RET: " + RET.ToString());
                }
                base_offset += 3;
            }
            var LEN = (int)Bytes.Read(Bytes.Split(data, 7, 2));
            var P_LEN = LEN + 2;
            if (await stream.ReadAsync(data, base_offset, P_LEN) != P_LEN)
            {
                throw new Exception("Partial packet read");
            }

            var PacketData = Bytes.Split(data, base_offset, LEN);
            var CRC = Bytes.Split(data, LEN + base_offset, 2);
            var ComputedCRC = (CRC[1] << 8) + CRC[0];
            var ExpectedCRC = CRC16.Compute(data, LEN + base_offset);
            if (ComputedCRC != ExpectedCRC)
            {
                throw new Exception("Invalid CRC");
            }
            return new Response(PacketData, Bytes.Read(CH));
        }
    }
}
