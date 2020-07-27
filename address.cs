﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Linq;
using System.Numerics;
using System.IO;

namespace VelasAddress
{
    class Program
    {
        static void Main(string[] args)
        {
            var ethAddresses = new[] {
                "0x32Be343B94f860124dC4fEe278FDCBD38C102D88",
                "0x000000000000000000000000000000000000000f",
                "0xf000000000000000000000000000000000000000",
                "0x0000000000000000000000000000000000000001",
                "0x1000000000000000000000000000000000000000",
                "0x0000000000000000000000000000000000000000",
                "0xffffffffffffffffffffffffffffffffffffffff",
                "0xf00000000000000000000000000000000000000f"
            };

            var vlxAddresses = new[] {
                "V5dJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f",
                "V111111111111111111111111112jSS6vy",
                "VNt1B3HD3MghPihCxhwMxNKRerBPPbiwvZ",
                "V111111111111111111111111111CdXjnE",
                "V2Tbp525fpnBRiSt4iPxXkxMyf5ZX7bGAJ",
                "V1111111111111111111111111113iMDfC",
                "VQLbz7JHiBTspS962RLKV8GndWFwdcRndD",
                "VNt1B3HD3MghPihCxhwMxNKRerBR4azAjj"
            };

            var success = true;

            Console.WriteLine("Check vlx -> eth");
            for (int i = 0; i < vlxAddresses.Count(); i++)
            {
                try
                {
                    var eth = new VelasAddress().vlxToEth(vlxAddresses[i]);
                    Console.WriteLine("vlx: {0} eth: {1}",vlxAddresses[i],eth);
                    if (eth != ethAddresses[i].ToLower())
                    {
                        success = false;
                        Console.WriteLine("failure {0}", vlxAddresses[i]);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine(success ? "success" : "failure");

            Console.WriteLine("-------------------");
            success = true;
            Console.WriteLine("Check eth -> vlx");
            for (int i = 0; i < ethAddresses.Count(); i++)
            {
                try
                {
                    var vlx = new VelasAddress().ethToVlx(ethAddresses[i]);
                    Console.WriteLine("vlx: {0} eth: {1}", vlx, ethAddresses[i]);

                    if (vlx != vlxAddresses[i])
                    {
                        success = false;
                        Console.WriteLine("failure {0}", ethAddresses[i]);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine(success ? "success" : "failure");
        }
    }

    class VelasAddress
    {
        private const string ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        private uint[] _lookup32;

        public VelasAddress()
        {
            _lookup32 = CreateLookup32();
        }

        private uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        public string ethToVlx(String address)
        {
            if ((address.Length == 0) || (address.Length != 42) || (address.Substring(0, 2).ToLower() != "0x"))
            {
                throw new Exception("Invalid address");
            }
            var cleanAddress = address.ToLower().Substring(2);
            var checkSum = sha256(sha256(cleanAddress)).Substring(0, 8);
            var longAddress = cleanAddress + checkSum;

            return "V" + bs58Encode(hexToByteArray(longAddress)).PadLeft(33, '1');
        }

        public string vlxToEth(String address)
        {
            if ((address.Length == 0) || (address[0] != 'V'))
            {
                throw new Exception("Invalid address");
            }
            var cleanAddress = address.Substring(1);
            var longAddress = bytesToHex(bs58Decode(cleanAddress)).ToLower();

            var pattern = "([0-9abcdef]+)([0-9abcdef]{8})";
            var match = Regex.Match(longAddress, pattern);

            if (match.Groups.Count != 3)
            {
                throw new Exception("Invalid address");
            }
            var matchValue = match.Groups[1].Value;
            while (matchValue.Length > 40)
            {
                if (matchValue[0] == '0')
                {
                    matchValue = matchValue.Substring(1);
                }
                else
                {
                    throw new Exception("Invalid address");
                }
            }
            var checkSum = sha256(sha256(matchValue)).Substring(0, 8);
            if (match.Groups[2].Value != checkSum)
            {
                throw new Exception("Invalid address");
            }

            String new_address = "0x" + matchValue;

            return new_address;
        }

        private static string bs58Encode(byte[] data)
        {
            // Decode byte[] to BigInteger
            var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

            // Encode BigInteger to Base58 string
            var result = string.Empty;
            while (intData > 0)
            {
                var remainder = (int)(intData % 58);
                intData /= 58;
                result = ALPHABET[remainder] + result;
            }

            // Append `1` for each leading 0 byte
            for (var i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = ALPHABET[0] + result;
            }

            return result;
        }

        private static byte[] bs58Decode(String data)
        {
            // Decode Base58 string to BigInteger 
            BigInteger intData = 0;
            for (int i = 0; i < data.Length; i++)
            {
                int digit = ALPHABET.IndexOf(data[i]); //Slow
                if (digit < 0)
                    throw new FormatException(string.Format("Invalid Base58 character `{0}` at position {1}", data[i], i));
                intData = intData * 58 + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            int leadingZeroCount = data.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros =
                intData.ToByteArray()
                .Reverse()// to big endian
                .SkipWhile(b => b == 0);//strip sign byte
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
            return result;
        }

        private string sha256(String rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        private byte[] hexToByteArray(String hex)
        {
            return Enumerable.Range(0, hex.Length)
                        .Where(x => x % 2 == 0)
                        .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                        .ToArray();
        }

        private string bytesToHex(byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);



            char[] c = new char[bytes.Length * 2];
            byte b;

            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b - 10 + 'A' : b + '0');

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b - 10 + 'A' : b + '0');
            }

            return new string(c);
        }
    }
}