using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Peloton.Simulation;

public static class StableSeedDerivation
{
    public const int ContractVersion = 1;

    public static ulong Derive(long masterSeed, string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        byte[] scopeBytes = Encoding.UTF8.GetBytes(scope);
        byte[] input = new byte[sizeof(long) + scopeBytes.Length];
        BinaryPrimitives.WriteInt64LittleEndian(input, masterSeed);
        scopeBytes.CopyTo(input, sizeof(long));
        byte[] digest = SHA256.HashData(input);
        return BinaryPrimitives.ReadUInt64LittleEndian(digest);
    }
}

public sealed class DeterministicRng
{
    private ulong state;

    public DeterministicRng(ulong seed)
    {
        state = seed;
    }

    public ulong NextUInt64()
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong value = state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    public double NextUnitInterval() => NextUInt64() / (double)ulong.MaxValue;

    public double NextSignedAmplitude(double amplitude) =>
        ((NextUnitInterval() * 2.0) - 1.0) * amplitude;
}
