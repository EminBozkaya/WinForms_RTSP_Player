/*
 * Project: WinForms RTSP Player - Gate Controller
 * Hardware: Arduino Uno R3, 433 MHz RF Transmitter
 * Library: RCSwitch (Install via Library Manager)
 * 
 * Connections:
 * RF Transmitter VCC -> Arduino 5V
 * RF Transmitter GND -> Arduino GND
 * RF Transmitter DATA -> Arduino Pin 10
 */

#include <RCSwitch.h>

RCSwitch mySwitch = RCSwitch();

// Serial Communication Settings
const long BAUD_RATE = 9600;
const char TRIGGER_CODE[] = "OPEN_GATE";

void setup() {
  Serial.begin(BAUD_RATE);
  
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  // RF Transmitter is connected to Pin 10
  mySwitch.enableTransmit(10);
  
  // Optional: Set protocol (default is 1)
  // mySwitch.setProtocol(1);
  
  // Optional: Set number of transmission repetitions
  mySwitch.setRepeatTransmit(15);

  Serial.println("GATE_CONTROLLER_READY");
  
  // Visual indicator: Ready
  for(int i=0; i<3; i++) {
    digitalWrite(LED_BUILTIN, HIGH); delay(100);
    digitalWrite(LED_BUILTIN, LOW); delay(100);
  }
}

void loop() {
  if (Serial.available() > 0) {
    String input = Serial.readStringUntil('\n');
    input.trim(); // Remove whitespace/newline

    if (input == TRIGGER_CODE) {
      Serial.println("EXECUTING_OPEN");
      
      // Visual indicator: Success
      digitalWrite(LED_BUILTIN, HIGH);
      
      // Send RF Code (Example: 123456)
      mySwitch.send(123456, 24); 
      
      delay(500); // Keep LED on for a bit
      digitalWrite(LED_BUILTIN, LOW);

      Serial.println("OPEN_DONE");
    }
  }
}