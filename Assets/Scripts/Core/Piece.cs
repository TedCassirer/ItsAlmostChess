using System;

namespace Core {
    public static class Piece {
        public const int None = 0;
        public const int Pawn = 1;
        public const int Knight = 2;
        public const int Bishop = 3;
        public const int Rook = 4;
        public const int Queen = 5;
        public const int King = 6;

        public const int White = 8;
        public const int Black = 16;

        private const int pieceMask = 0b00111;
        private const int whiteMask = 0b01000;
        private const int blackMask = 0b10000;
        private const int colorMask = whiteMask | blackMask;


        public static int Color(int piece) {
            return piece & colorMask;
        }

        public static bool IsColor(int piece, int color) {
            return (piece & color) == color;
        }

        public static int Type(int piece) {
            return piece & pieceMask;
        }

        public static int FromFENSymbol(char symbol) {
            var colorMask = char.IsUpper(symbol) ? whiteMask : blackMask;
            switch (char.ToLower(symbol)) {
                case 'p':
                    return Pawn | colorMask;
                case 'b':
                    return Bishop | colorMask;
                case 'n':
                    return Knight | colorMask;
                case 'r':
                    return Rook | colorMask;
                case 'q':
                    return Queen | colorMask;
                case 'k':
                    return King | colorMask;
                default:
                    throw new Exception("Unknown piece symbol " + symbol);
            }
        }
    }
}