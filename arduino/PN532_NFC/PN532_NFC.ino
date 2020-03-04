#include <Wire.h>
#include <PN532_I2C.h>
#include <PN532.h>
#include <NfcAdapter.h>

PN532_I2C pn532i2c(Wire);
PN532 nfc(pn532i2c);

/* Uno's A4 to SDA & A5 to SCL */

String tmp_res = "";

void setup(void) {
    Serial.begin(9600);
    Serial.println("NDEF Reader");
    nfc.begin();
}

void loop(void) {
    uint8_t uid[] = { 0, 0, 0, 0, 0, 0, 0 };
    uint8_t uidLength;
  
    if (nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, &uid[0], &uidLength, 50)) {
      tmp_res = "";
      for (uint8_t i = 0; i < uidLength; i++) {
        tmp_res.concat(String(uid[i], HEX));
      }
      Serial.println(tmp_res);  
    }
    
    delay(1000);
}
