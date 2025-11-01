using System;

namespace Utils {
    public static class BoardUtils {
        private static string[] fileNames = { "a", "b", "c", "d", "e", "f", "g", "h" };
        public static string SquareName(int file, int rank) {
            return fileNames[file] + (rank + 1);
        }

        public static bool IsLightSquare(int file, int rank) {
            return (file + rank) % 2 == 1;
        }
    }
    
}