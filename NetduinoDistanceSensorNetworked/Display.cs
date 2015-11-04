using System;
using Microsoft.SPOT;
using MicroLiquidCrystal;

namespace LanderNetduino
{
    public static class Display
    {
        private static Lcd _lcd;

        public static void DisplaySetup(GpioLcdTransferProvider lcdProvider)
        {
            _lcd = new Lcd(lcdProvider);            
        }

        public static void DisplayMessage(String msgLine1, String msgLine2)
        {
            _lcd.Begin(16, 2);
            _lcd.Write(msgLine1);
            _lcd.SetCursorPosition(0, 1);
            _lcd.Write(msgLine2);            
        }

        public static void DisplayBlink()
        {
            _lcd.BlinkCursor = true;
        }

    }
}
