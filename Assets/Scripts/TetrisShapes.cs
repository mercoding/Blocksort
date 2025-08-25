public static class TetrisShapes
{
    // I-Block (4x4)
    public static readonly bool[,] I = new bool[4, 4]
    {
        { false, false, false, false },
        { false, false, false, false },
        { false, false, false, false },
        { true,  true,  true,  true  }
    };

    // O-Block (2x2)
    public static readonly bool[,] O = new bool[2, 2]
    {
        { true, true },
        { true, true }
    };

    // T-Block (3x3)
    public static readonly bool[,] T = new bool[3, 3]
    {
        { false, false, false },
        { true,  true,  true  },
        { false, true,  false }
    };

    // L-Block (3x3)
    public static readonly bool[,] L = new bool[3, 3]
    {
        { false, false, true  },
        { true,  true,  true  },
        { false, false, false }
    };

    // J-Block (3x3)
    public static readonly bool[,] J = new bool[3, 3]
    {
        { true,  false, false },
        { true,  true,  true  },
        { false, false, false }
    };

    // S-Block (3x3)
    public static readonly bool[,] S = new bool[3, 3]
    {
        { false, true,  true  },
        { true,  true,  false },
        { false, false, false }
    };

    // Z-Block (3x3)
    public static readonly bool[,] Z = new bool[3, 3]
    {
        { true,  true,  false },
        { false, true,  true  },
        { false, false, false }
    };
}