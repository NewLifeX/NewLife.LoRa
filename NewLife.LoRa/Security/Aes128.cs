using System;
using System.Linq;

namespace NewLife.LoRa.Security;

class Aes128
{
    const Int32 N_ROW = 4;
    const Int32 N_COL = 4;
    const Int32 N_BLOCK = (N_ROW * N_COL);
    const Int32 N_MAX_ROUNDS = 14;

    public Byte[] ksch = new Byte[(N_MAX_ROUNDS + 1) * N_BLOCK];
    public Int32 rnd;

    #region 构造
    static Aes128() => InitTable();
    #endregion

    public Int32 SetKey(Byte[] key)
    {
        var keylen = key.Length;

        switch (keylen)
        {
            case 16:
            case 24:
            case 32:
                break;
            default:
                rnd = 0;
                return -1;
        }
        //block_copy_nn(ksch, key, keylen);
        ksch.Write(0, key);
        var hi = (keylen + 28) << 2;
        rnd = (hi >> 4) - 1;

        var rc = 1;
        for (var cc = keylen; cc < hi; cc += 4)
        {
            Byte tt, t0, t1, t2, t3;

            t0 = ksch[cc - 4];
            t1 = ksch[cc - 3];
            t2 = ksch[cc - 2];
            t3 = ksch[cc - 1];
            if (cc % keylen == 0)
            {
                tt = t0;
                t0 = (Byte)(s_box(t1) ^ rc);
                t1 = s_box(t2);
                t2 = s_box(t3);
                t3 = s_box(tt);
                rc = f2((Byte)rc);
            }
            else if (keylen > 24 && cc % keylen == 16)
            {
                t0 = s_box(t0);
                t1 = s_box(t1);
                t2 = s_box(t2);
                t3 = s_box(t3);
            }
            tt = (Byte)(cc - keylen);
            ksch[cc + 0] = (Byte)(ksch[tt + 0] ^ t0);
            ksch[cc + 1] = (Byte)(ksch[tt + 1] ^ t1);
            ksch[cc + 2] = (Byte)(ksch[tt + 2] ^ t2);
            ksch[cc + 3] = (Byte)(ksch[tt + 3] ^ t3);
        }
        return 0;
    }

    public Byte[] Encrypt(Byte[] data, Int32 length)
    {
        //Byte s1[N_BLOCK], r;
        //var s1 = new Byte[N_BLOCK];
        //copy_and_key(s1, in, ksch);
        var s1 = copy_and_key(data, ksch, 0);

        var r = 0;
        for (r = 1; r < rnd; ++r)
        {
            mix_sub_columns(s1);
            add_round_key(s1, ksch, r * N_BLOCK);
        }
        shift_sub_rows(s1);
        //copy_and_key(out, s1, ksch + r * N_BLOCK);
        var rs = copy_and_key(s1, ksch, r * N_BLOCK);

        return rs;
    }

    public Byte[] Decrypt(Byte[] data, Int32 length)
    {
        //Byte s1[N_BLOCK], r;
        //var s1 = new Byte[N_BLOCK];
        //copy_and_key(s1, in, ksch + rnd * N_BLOCK);
        var s1 = copy_and_key(data, ksch, rnd * N_BLOCK);

        inv_shift_sub_rows(s1);

        for (var r = rnd; --r > 0;)
        {
            add_round_key(s1, ksch, r * N_BLOCK);
            inv_mix_sub_columns(s1);
        }
        //copy_and_key(out, s1, ksch);
        var rs = copy_and_key(s1, ksch, 0);

        return rs;
    }

    #region 查表
    const Int32 WPOLY = 0x011b;
    const Int32 BPOLY = 0x1b;
    const Int32 DPOLY = 0x008d;

    static Byte f1(Byte x) => (x);
    static Byte f2(Byte x) => (Byte)((x << 1) ^ (((x >> 7) & 1) * WPOLY));
    static Byte f4(Byte x) => (Byte)((x << 2) ^ (((x >> 6) & 1) * WPOLY) ^ (((x >> 6) & 2) * WPOLY));
    static Byte f8(Byte x) => (Byte)((x << 3) ^ (((x >> 5) & 1) * WPOLY) ^ (((x >> 5) & 2) * WPOLY) ^ (((x >> 5) & 4) * WPOLY));
    static Byte d2(Byte x) => (Byte)(((x) >> 1) ^ (((x & 1) == 1) ? DPOLY : 0));

    static Byte f3(Byte x) => (Byte)(f2(x) ^ x);
    static Byte f9(Byte x) => (Byte)(f8(x) ^ x);
    static Byte fb(Byte x) => (Byte)(f8(x) ^ f2(x) ^ x);
    static Byte fd(Byte x) => (Byte)(f8(x) ^ f4(x) ^ x);
    static Byte fe(Byte x) => (Byte)(f8(x) ^ f4(x) ^ f2(x));

    static Byte s_box(Int32 x) => sbox[x];
    static Byte is_box(Int32 x) => isbox[x];
    static Byte gfm2_sb(Int32 x) => gfm2_sbox[x];
    static Byte gfm3_sb(Int32 x) => gfm3_sbox[x];
    static Byte gfm_9(Int32 x) => gfmul_9[x];
    static Byte gfm_b(Int32 x) => gfmul_b[x];
    static Byte gfm_d(Int32 x) => gfmul_d[x];
    static Byte gfm_e(Int32 x) => gfmul_e[x];

    static Byte[] sbox;
    static Byte[] isbox;

    static Byte[] gfm2_sbox;
    static Byte[] gfm3_sbox;

    static Byte[] gfmul_9;
    static Byte[] gfmul_b;
    static Byte[] gfmul_d;
    static Byte[] gfmul_e;

    static void InitTable()
    {
        sbox = sb_data.Select(f1).ToArray();
        isbox = isb_data.Select(f1).ToArray();

        gfm2_sbox = sb_data.Select(f2).ToArray();
        gfm3_sbox = sb_data.Select(f3).ToArray();

        gfmul_9 = mm_data.Select(f9).ToArray();
        gfmul_b = mm_data.Select(fb).ToArray();
        gfmul_d = mm_data.Select(fd).ToArray();
        gfmul_e = mm_data.Select(fe).ToArray();
    }

    /// <summary>S Box data values</summary>
    static readonly Byte[] sb_data = new Byte[]{
0x63,0x7c,0x77,0x7b,0xf2,0x6b,0x6f,0xc5,
0x30,0x01,0x67,0x2b,0xfe,0xd7,0xab,0x76,
0xca,0x82,0xc9,0x7d,0xfa,0x59,0x47,0xf0,
0xad,0xd4,0xa2,0xaf,0x9c,0xa4,0x72,0xc0,
0xb7,0xfd,0x93,0x26,0x36,0x3f,0xf7,0xcc,
0x34,0xa5,0xe5,0xf1,0x71,0xd8,0x31,0x15,
0x04,0xc7,0x23,0xc3,0x18,0x96,0x05,0x9a,
0x07,0x12,0x80,0xe2,0xeb,0x27,0xb2,0x75,
0x09,0x83,0x2c,0x1a,0x1b,0x6e,0x5a,0xa0,
0x52,0x3b,0xd6,0xb3,0x29,0xe3,0x2f,0x84,
0x53,0xd1,0x00,0xed,0x20,0xfc,0xb1,0x5b,
0x6a,0xcb,0xbe,0x39,0x4a,0x4c,0x58,0xcf,
0xd0,0xef,0xaa,0xfb,0x43,0x4d,0x33,0x85,
0x45,0xf9,0x02,0x7f,0x50,0x3c,0x9f,0xa8,
0x51,0xa3,0x40,0x8f,0x92,0x9d,0x38,0xf5,
0xbc,0xb6,0xda,0x21,0x10,0xff,0xf3,0xd2,
0xcd,0x0c,0x13,0xec,0x5f,0x97,0x44,0x17,
0xc4,0xa7,0x7e,0x3d,0x64,0x5d,0x19,0x73,
0x60,0x81,0x4f,0xdc,0x22,0x2a,0x90,0x88,
0x46,0xee,0xb8,0x14,0xde,0x5e,0x0b,0xdb,
0xe0,0x32,0x3a,0x0a,0x49,0x06,0x24,0x5c,
0xc2,0xd3,0xac,0x62,0x91,0x95,0xe4,0x79,
0xe7,0xc8,0x37,0x6d,0x8d,0xd5,0x4e,0xa9,
0x6c,0x56,0xf4,0xea,0x65,0x7a,0xae,0x08,
0xba,0x78,0x25,0x2e,0x1c,0xa6,0xb4,0xc6,
0xe8,0xdd,0x74,0x1f,0x4b,0xbd,0x8b,0x8a,
0x70,0x3e,0xb5,0x66,0x48,0x03,0xf6,0x0e,
0x61,0x35,0x57,0xb9,0x86,0xc1,0x1d,0x9e,
0xe1,0xf8,0x98,0x11,0x69,0xd9,0x8e,0x94,
0x9b,0x1e,0x87,0xe9,0xce,0x55,0x28,0xdf,
0x8c,0xa1,0x89,0x0d,0xbf,0xe6,0x42,0x68,
0x41,0x99,0x2d,0x0f,0xb0,0x54,0xbb,0x16 };

    /// <summary>inverse S Box data values</summary>
    static readonly Byte[] isb_data = new Byte[] {
0x52,0x09,0x6a,0xd5,0x30,0x36,0xa5,0x38,
0xbf,0x40,0xa3,0x9e,0x81,0xf3,0xd7,0xfb,
0x7c,0xe3,0x39,0x82,0x9b,0x2f,0xff,0x87,
0x34,0x8e,0x43,0x44,0xc4,0xde,0xe9,0xcb,
0x54,0x7b,0x94,0x32,0xa6,0xc2,0x23,0x3d,
0xee,0x4c,0x95,0x0b,0x42,0xfa,0xc3,0x4e,
0x08,0x2e,0xa1,0x66,0x28,0xd9,0x24,0xb2,
0x76,0x5b,0xa2,0x49,0x6d,0x8b,0xd1,0x25,
0x72,0xf8,0xf6,0x64,0x86,0x68,0x98,0x16,
0xd4,0xa4,0x5c,0xcc,0x5d,0x65,0xb6,0x92,
0x6c,0x70,0x48,0x50,0xfd,0xed,0xb9,0xda,
0x5e,0x15,0x46,0x57,0xa7,0x8d,0x9d,0x84,
0x90,0xd8,0xab,0x00,0x8c,0xbc,0xd3,0x0a,
0xf7,0xe4,0x58,0x05,0xb8,0xb3,0x45,0x06,
0xd0,0x2c,0x1e,0x8f,0xca,0x3f,0x0f,0x02,
0xc1,0xaf,0xbd,0x03,0x01,0x13,0x8a,0x6b,
0x3a,0x91,0x11,0x41,0x4f,0x67,0xdc,0xea,
0x97,0xf2,0xcf,0xce,0xf0,0xb4,0xe6,0x73,
0x96,0xac,0x74,0x22,0xe7,0xad,0x35,0x85,
0xe2,0xf9,0x37,0xe8,0x1c,0x75,0xdf,0x6e,
0x47,0xf1,0x1a,0x71,0x1d,0x29,0xc5,0x89,
0x6f,0xb7,0x62,0x0e,0xaa,0x18,0xbe,0x1b,
0xfc,0x56,0x3e,0x4b,0xc6,0xd2,0x79,0x20,
0x9a,0xdb,0xc0,0xfe,0x78,0xcd,0x5a,0xf4,
0x1f,0xdd,0xa8,0x33,0x88,0x07,0xc7,0x31,
0xb1,0x12,0x10,0x59,0x27,0x80,0xec,0x5f,
0x60,0x51,0x7f,0xa9,0x19,0xb5,0x4a,0x0d,
0x2d,0xe5,0x7a,0x9f,0x93,0xc9,0x9c,0xef,
0xa0,0xe0,0x3b,0x4d,0xae,0x2a,0xf5,0xb0,
0xc8,0xeb,0xbb,0x3c,0x83,0x53,0x99,0x61,
0x17,0x2b,0x04,0x7e,0xba,0x77,0xd6,0x26,
0xe1,0x69,0x14,0x63,0x55,0x21,0x0c,0x7d};

    /// <summary>basic data for forming finite field tables</summary>
    static readonly Byte[] mm_data = new Byte[] {
0x00,0x01,0x02,0x03,0x04,0x05,0x06,0x07,
0x08,0x09,0x0a,0x0b,0x0c,0x0d,0x0e,0x0f,
0x10,0x11,0x12,0x13,0x14,0x15,0x16,0x17,
0x18,0x19,0x1a,0x1b,0x1c,0x1d,0x1e,0x1f,
0x20,0x21,0x22,0x23,0x24,0x25,0x26,0x27,
0x28,0x29,0x2a,0x2b,0x2c,0x2d,0x2e,0x2f,
0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,
0x38,0x39,0x3a,0x3b,0x3c,0x3d,0x3e,0x3f,
0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,
0x48,0x49,0x4a,0x4b,0x4c,0x4d,0x4e,0x4f,
0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,
0x58,0x59,0x5a,0x5b,0x5c,0x5d,0x5e,0x5f,
0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,
0x68,0x69,0x6a,0x6b,0x6c,0x6d,0x6e,0x6f,
0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,
0x78,0x79,0x7a,0x7b,0x7c,0x7d,0x7e,0x7f,
0x80,0x81,0x82,0x83,0x84,0x85,0x86,0x87,
0x88,0x89,0x8a,0x8b,0x8c,0x8d,0x8e,0x8f,
0x90,0x91,0x92,0x93,0x94,0x95,0x96,0x97,
0x98,0x99,0x9a,0x9b,0x9c,0x9d,0x9e,0x9f,
0xa0,0xa1,0xa2,0xa3,0xa4,0xa5,0xa6,0xa7,
0xa8,0xa9,0xaa,0xab,0xac,0xad,0xae,0xaf,
0xb0,0xb1,0xb2,0xb3,0xb4,0xb5,0xb6,0xb7,
0xb8,0xb9,0xba,0xbb,0xbc,0xbd,0xbe,0xbf,
0xc0,0xc1,0xc2,0xc3,0xc4,0xc5,0xc6,0xc7,
0xc8,0xc9,0xca,0xcb,0xcc,0xcd,0xce,0xcf,
0xd0,0xd1,0xd2,0xd3,0xd4,0xd5,0xd6,0xd7,
0xd8,0xd9,0xda,0xdb,0xdc,0xdd,0xde,0xdf,
0xe0,0xe1,0xe2,0xe3,0xe4,0xe5,0xe6,0xe7,
0xe8,0xe9,0xea,0xeb,0xec,0xed,0xee,0xef,
0xf0,0xf1,0xf2,0xf3,0xf4,0xf5,0xf6,0xf7,
0xf8,0xf9,0xfa,0xfb,0xfc,0xfd,0xfe,0xff };
    #endregion

    #region 辅助
    static void xor_block(Byte[] data, Byte[] key, Int32 keyOffset)
    {
        for (var i = 0; i < data.Length; i++)
        {
            data[i] ^= key[keyOffset + i];
        }

        //#if defined( HAVE_UINT_32T )
        //	((uint32_t*)d)[0] ^= ((uint32_t*)s)[0];
        //	((uint32_t*)d)[1] ^= ((uint32_t*)s)[1];
        //	((uint32_t*)d)[2] ^= ((uint32_t*)s)[2];
        //	((uint32_t*)d)[3] ^= ((uint32_t*)s)[3];
        //#else
        //        ((uint8_t*) d)[0] ^= ((uint8_t*) s)[0];
        //	((uint8_t*) d)[1] ^= ((uint8_t*) s)[1];
        //	((uint8_t*) d)[2] ^= ((uint8_t*) s)[2];
        //	((uint8_t*) d)[3] ^= ((uint8_t*) s)[3];
        //	((uint8_t*) d)[4] ^= ((uint8_t*) s)[4];
        //	((uint8_t*) d)[5] ^= ((uint8_t*) s)[5];
        //	((uint8_t*) d)[6] ^= ((uint8_t*) s)[6];
        //	((uint8_t*) d)[7] ^= ((uint8_t*) s)[7];
        //	((uint8_t*) d)[8] ^= ((uint8_t*) s)[8];
        //	((uint8_t*) d)[9] ^= ((uint8_t*) s)[9];
        //	((uint8_t*) d)[10] ^= ((uint8_t*) s)[10];
        //	((uint8_t*) d)[11] ^= ((uint8_t*) s)[11];
        //	((uint8_t*) d)[12] ^= ((uint8_t*) s)[12];
        //	((uint8_t*) d)[13] ^= ((uint8_t*) s)[13];
        //	((uint8_t*) d)[14] ^= ((uint8_t*) s)[14];
        //	((uint8_t*) d)[15] ^= ((uint8_t*) s)[15];
        //#endif
    }

    static void add_round_key(Byte[] data, Byte[] key, Int32 keyOffset) => xor_block(data, key, keyOffset);

    /// <summary>异或运算</summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="keyOffset"></param>
    /// <returns></returns>
    static Byte[] copy_and_key(Byte[] data, Byte[] key, Int32 keyOffset)
    {
        var rs = new Byte[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            rs[i] = (Byte)(data[i] ^ key[keyOffset + i]);
        }

        return rs;

        //#if defined( HAVE_UINT_32T )
        //        ((uint32_t*) d)[0] = ((uint32_t*) s)[0] ^ ((uint32_t*) k)[0];
        //	((uint32_t*) d)[1] = ((uint32_t*) s)[1] ^ ((uint32_t*) k)[1];
        //	((uint32_t*) d)[2] = ((uint32_t*) s)[2] ^ ((uint32_t*) k)[2];
        //	((uint32_t*) d)[3] = ((uint32_t*) s)[3] ^ ((uint32_t*) k)[3];
        //#elif 1
        //	((uint8_t*) d)[0] = ((uint8_t*) s)[0] ^ ((uint8_t*) k)[0];
        //	((uint8_t*) d)[1] = ((uint8_t*) s)[1] ^ ((uint8_t*) k)[1];
        //	((uint8_t*) d)[2] = ((uint8_t*) s)[2] ^ ((uint8_t*) k)[2];
        //	((uint8_t*) d)[3] = ((uint8_t*) s)[3] ^ ((uint8_t*) k)[3];
        //	((uint8_t*) d)[4] = ((uint8_t*) s)[4] ^ ((uint8_t*) k)[4];
        //	((uint8_t*) d)[5] = ((uint8_t*) s)[5] ^ ((uint8_t*) k)[5];
        //	((uint8_t*) d)[6] = ((uint8_t*) s)[6] ^ ((uint8_t*) k)[6];
        //	((uint8_t*) d)[7] = ((uint8_t*) s)[7] ^ ((uint8_t*) k)[7];
        //	((uint8_t*) d)[8] = ((uint8_t*) s)[8] ^ ((uint8_t*) k)[8];
        //	((uint8_t*) d)[9] = ((uint8_t*) s)[9] ^ ((uint8_t*) k)[9];
        //	((uint8_t*) d)[10] = ((uint8_t*) s)[10] ^ ((uint8_t*) k)[10];
        //	((uint8_t*) d)[11] = ((uint8_t*) s)[11] ^ ((uint8_t*) k)[11];
        //	((uint8_t*) d)[12] = ((uint8_t*) s)[12] ^ ((uint8_t*) k)[12];
        //	((uint8_t*) d)[13] = ((uint8_t*) s)[13] ^ ((uint8_t*) k)[13];
        //	((uint8_t*) d)[14] = ((uint8_t*) s)[14] ^ ((uint8_t*) k)[14];
        //	((uint8_t*) d)[15] = ((uint8_t*) s)[15] ^ ((uint8_t*) k)[15];
        //#else
        //            block_copy(d, s);
        //            xor_block(d, k);
        //#endif
    }

    static void mix_sub_columns(Byte[] dt)
    {
        //uint8_t st[N_BLOCK];
        //block_copy(st, dt);
        var st = dt.ReadBytes(0, -1);

        dt[0] = (Byte)(gfm2_sb(st[0]) ^ gfm3_sb(st[5]) ^ s_box(st[10]) ^ s_box(st[15]));
        dt[1] = (Byte)(s_box(st[0]) ^ gfm2_sb(st[5]) ^ gfm3_sb(st[10]) ^ s_box(st[15]));
        dt[2] = (Byte)(s_box(st[0]) ^ s_box(st[5]) ^ gfm2_sb(st[10]) ^ gfm3_sb(st[15]));
        dt[3] = (Byte)(gfm3_sb(st[0]) ^ s_box(st[5]) ^ s_box(st[10]) ^ gfm2_sb(st[15]));

        dt[4] = (Byte)(gfm2_sb(st[4]) ^ gfm3_sb(st[9]) ^ s_box(st[14]) ^ s_box(st[3]));
        dt[5] = (Byte)(s_box(st[4]) ^ gfm2_sb(st[9]) ^ gfm3_sb(st[14]) ^ s_box(st[3]));
        dt[6] = (Byte)(s_box(st[4]) ^ s_box(st[9]) ^ gfm2_sb(st[14]) ^ gfm3_sb(st[3]));
        dt[7] = (Byte)(gfm3_sb(st[4]) ^ s_box(st[9]) ^ s_box(st[14]) ^ gfm2_sb(st[3]));

        dt[8] = (Byte)(gfm2_sb(st[8]) ^ gfm3_sb(st[13]) ^ s_box(st[2]) ^ s_box(st[7]));
        dt[9] = (Byte)(s_box(st[8]) ^ gfm2_sb(st[13]) ^ gfm3_sb(st[2]) ^ s_box(st[7]));
        dt[10] = (Byte)(s_box(st[8]) ^ s_box(st[13]) ^ gfm2_sb(st[2]) ^ gfm3_sb(st[7]));
        dt[11] = (Byte)(gfm3_sb(st[8]) ^ s_box(st[13]) ^ s_box(st[2]) ^ gfm2_sb(st[7]));

        dt[12] = (Byte)(gfm2_sb(st[12]) ^ gfm3_sb(st[1]) ^ s_box(st[6]) ^ s_box(st[11]));
        dt[13] = (Byte)(s_box(st[12]) ^ gfm2_sb(st[1]) ^ gfm3_sb(st[6]) ^ s_box(st[11]));
        dt[14] = (Byte)(s_box(st[12]) ^ s_box(st[1]) ^ gfm2_sb(st[6]) ^ gfm3_sb(st[11]));
        dt[15] = (Byte)(gfm3_sb(st[12]) ^ s_box(st[1]) ^ s_box(st[6]) ^ gfm2_sb(st[11]));
    }

    static void inv_mix_sub_columns(Byte[] dt)
    {
        //uint8_t st[N_BLOCK];
        //block_copy(st, dt);
        var st = dt.ReadBytes(0, -1);

        dt[0] = is_box(gfm_e(st[0]) ^ gfm_b(st[1]) ^ gfm_d(st[2]) ^ gfm_9(st[3]));
        dt[5] = is_box(gfm_9(st[0]) ^ gfm_e(st[1]) ^ gfm_b(st[2]) ^ gfm_d(st[3]));
        dt[10] = is_box(gfm_d(st[0]) ^ gfm_9(st[1]) ^ gfm_e(st[2]) ^ gfm_b(st[3]));
        dt[15] = is_box(gfm_b(st[0]) ^ gfm_d(st[1]) ^ gfm_9(st[2]) ^ gfm_e(st[3]));

        dt[4] = is_box(gfm_e(st[4]) ^ gfm_b(st[5]) ^ gfm_d(st[6]) ^ gfm_9(st[7]));
        dt[9] = is_box(gfm_9(st[4]) ^ gfm_e(st[5]) ^ gfm_b(st[6]) ^ gfm_d(st[7]));
        dt[14] = is_box(gfm_d(st[4]) ^ gfm_9(st[5]) ^ gfm_e(st[6]) ^ gfm_b(st[7]));
        dt[3] = is_box(gfm_b(st[4]) ^ gfm_d(st[5]) ^ gfm_9(st[6]) ^ gfm_e(st[7]));

        dt[8] = is_box(gfm_e(st[8]) ^ gfm_b(st[9]) ^ gfm_d(st[10]) ^ gfm_9(st[11]));
        dt[13] = is_box(gfm_9(st[8]) ^ gfm_e(st[9]) ^ gfm_b(st[10]) ^ gfm_d(st[11]));
        dt[2] = is_box(gfm_d(st[8]) ^ gfm_9(st[9]) ^ gfm_e(st[10]) ^ gfm_b(st[11]));
        dt[7] = is_box(gfm_b(st[8]) ^ gfm_d(st[9]) ^ gfm_9(st[10]) ^ gfm_e(st[11]));

        dt[12] = is_box(gfm_e(st[12]) ^ gfm_b(st[13]) ^ gfm_d(st[14]) ^ gfm_9(st[15]));
        dt[1] = is_box(gfm_9(st[12]) ^ gfm_e(st[13]) ^ gfm_b(st[14]) ^ gfm_d(st[15]));
        dt[6] = is_box(gfm_d(st[12]) ^ gfm_9(st[13]) ^ gfm_e(st[14]) ^ gfm_b(st[15]));
        dt[11] = is_box(gfm_b(st[12]) ^ gfm_d(st[13]) ^ gfm_9(st[14]) ^ gfm_e(st[15]));
    }


    static void shift_sub_rows(Byte[] st)
    {
        Byte tt;

        st[0] = s_box(st[0]); st[4] = s_box(st[4]);
        st[8] = s_box(st[8]); st[12] = s_box(st[12]);

        tt = st[1]; st[1] = s_box(st[5]); st[5] = s_box(st[9]);
        st[9] = s_box(st[13]); st[13] = s_box(tt);

        tt = st[2]; st[2] = s_box(st[10]); st[10] = s_box(tt);
        tt = st[6]; st[6] = s_box(st[14]); st[14] = s_box(tt);

        tt = st[15]; st[15] = s_box(st[11]); st[11] = s_box(st[7]);
        st[7] = s_box(st[3]); st[3] = s_box(tt);
    }


    static void inv_shift_sub_rows(Byte[] st)
    {
        Byte tt;

        st[0] = is_box(st[0]); st[4] = is_box(st[4]);
        st[8] = is_box(st[8]); st[12] = is_box(st[12]);

        tt = st[13]; st[13] = is_box(st[9]); st[9] = is_box(st[5]);
        st[5] = is_box(st[1]); st[1] = is_box(tt);

        tt = st[2]; st[2] = is_box(st[10]); st[10] = is_box(tt);
        tt = st[6]; st[6] = is_box(st[14]); st[14] = is_box(tt);

        tt = st[3]; st[3] = is_box(st[7]); st[7] = is_box(st[11]);
        st[11] = is_box(st[15]); st[15] = is_box(tt);
    }

    #endregion
}