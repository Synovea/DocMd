using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public static class StringExtensions
{
    public static byte[] GetHash(this string graph)
    {
        HashAlgorithm algorithm = SHA256.Create();  //or use SHA256.Create();
        return algorithm.ComputeHash(Encoding.UTF8.GetBytes(graph));
    }

    public static string GetHashString(this string graph)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in GetHash(graph))
            sb.Append(b.ToString("X2"));

        return sb.ToString();
    }
}
