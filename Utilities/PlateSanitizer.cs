using System.Text.RegularExpressions;

namespace WinForms_RTSP_Player.Utilities
{
    public static class PlateSanitizer
    {
        //Not: TR plaka satandartları: 
        //"01-81 X 9999",
        //"01-81 X 99999"
        //
        //"01-81 XX 999",
        //"01-81 XX 9999"
        //
        //"01-81 XXX 99"
        //"01-81 XXX 999"



        // Ana metod: Tüm işlemleri adım adım yapar
        public static string ValidateTurkishPlateFormat(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return null;

            plate = plate.ToUpperInvariant().Trim();

            plate = RemoveInvalidCharacters(plate);
            plate = FixFirstTwoCharacters(plate);
            plate = FixThirdCharacterIfPossible(plate);
            plate = FixLastBlockDigits(plate);

            //if (!IsValidTurkishPlateFormat(plate))
            //    return null;

            return plate;
        }

        // Örnek: sadece A-Z ve 0-9 karakterleri kalır, diğerleri atılır
        private static string RemoveInvalidCharacters(string plate)
        {
            var regex = new Regex("[^A-Z0-9]");
            return regex.Replace(plate, "");
        }



        
        /// <summary>
        /// İlk iki karakter rakam olmalıdır. ilk iki karakterde OCR hatalarını düzeltir - varsa 'O','S' gibi harfleri rakamlarla değiştirir
        /// </summary>
        /// <param name="plate"></param>
        /// <returns></returns>
        private static string FixFirstTwoCharacters(string plate)
        {
            if (string.IsNullOrEmpty(plate) || plate.Length < 2)
                return plate;

            char[] chars = plate.ToCharArray();

            // Eğer toplam karakter sayısı 7 veya daha fazla ise ilk iki karakterde rakam düzeltmesi yapabiliriz
            if (plate.Length >= 7)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (!char.IsDigit(chars[i]))
                    {
                        chars[i] = chars[i].ToDigitCorrection(); // Extension method kullanılıyor
                    }
                }
            }

            return new string(chars);
        }

        /// <summary>
        /// Plakadaki 3. karakter harf olmak zorundadır. Eğer rakamsa veya güvenilir olmayan bir karakter ise düzeltir.
        /// </summary>
        /// <param name="plate"></param>
        /// <returns></returns>
        private static string FixThirdCharacterIfPossible(string plate)
        {
            if (string.IsNullOrEmpty(plate) || plate.Length < 7)
                return plate;

            char[] chars = plate.ToCharArray();

            if (chars.Length >= 3)
            {
                chars[2] = chars[2].ToLetterCorrection(); // yeni extension metod
            }

            return new string(chars);
        }


        /// <summary>
        /// Son 4 veya 5 bloktaki rakamları düzeltir.
        /// </summary>
        /// <param name="plate"></param>
        /// <returns></returns>
        private static string FixLastBlockDigits(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return plate;

            char[] chars = plate.ToCharArray();
            int length = plate.Length;

            if (length == 7)
            {
                // En az son 2 karakter rakam olmalı → düzelt
                FixRightCharacters(chars, 5, 7);

                // indeks 3'e bak → rakamsa ve güvenilirse, indeks 4'ü düzelt ve çık
                if (char.IsDigit(chars[3]) && !IsSuspectDigit(chars[3]))
                {
                    chars[4] = chars[4].ToDigitCorrection();
                    return new string(chars);
                }
            }
            else if (length == 8)
            {
                // En az son 3 karakter rakam olmalı → düzelt
                FixRightCharacters(chars, 5, 8);

                // indeks 3'e bak → rakamsa ve güvenilirse, indeks 4'ü düzelt ve çık
                if (char.IsDigit(chars[3]) && !IsSuspectDigit(chars[3]))
                {
                    chars[4] = chars[4].ToDigitCorrection();
                    return new string(chars);
                }
            }

            return new string(chars);
        }







        //// Diğer Yardımcı Metotlar:

        // Güvenilirliği şüpheli rakamlar (OCR'da harfle karışma ihtimali olanlar)
        private static bool IsSuspectDigit(char c)
        {
            return c == '0' || c == '8' || c == '5' || c == '2' || c == '1';
        }

        // Belirli aralıktaki ilk rakamı bulur
        private static int FindFirstDigitIndex(char[] chars, int startIndex)
        {
            for (int i = startIndex; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]))
                    return i;
            }
            return -1;
        }

        // Belirli aralıktaki harf karakterleri düzeltir
        private static void FixRightCharacters(char[] chars, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                chars[i] = chars[i].ToDigitCorrection(); // extension method
            }
        }


        // Basit bir Türk plaka formatı kontrolü (01ABC123 gibi)
        // İstersen burayı ihtiyaca göre regex ile geliştirebiliriz
        private static bool IsValidTurkishPlateFormat(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return false;

            // Türkiye plakaları genellikle 6-8 karakter arası olur
            if (plate.Length < 6 || plate.Length > 8)
                return false;

            // Örnek regex: 2 rakam + 1-3 harf + 2-4 rakam (genel format)
            var regex = new Regex(@"^\d{2}[A-Z]{1,3}\d{2,4}$");
            return regex.IsMatch(plate);
        }
    }
}
