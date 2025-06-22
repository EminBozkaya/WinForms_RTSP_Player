namespace WinForms_RTSP_Player.Utilities
{
    public static class CharExtensions
    {
        public static char ToDigitCorrection(this char c)
        {
            switch (c)
            {
                case 'O':
                case 'C':
                    return '0';
                case 'B':
                    return '8';
                case 'S':
                    return '5';
                case 'Z':
                    return '2';
                case 'I':
                    return '1';
                default:
                    return c;
            }
        }
        public static char ToLetterCorrection(this char c)
        {
            switch (c)
            {
                case '0':
                    return 'O';
                case '8':
                    return 'B';
                case '5':
                    return 'S';
                case '2':
                    return 'Z';
                case '1':
                    return 'I';
                default:
                    return c;
            }
        }
    }
}
