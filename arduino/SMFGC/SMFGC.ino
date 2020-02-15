#include <SPI.h>
#include <Wire.h>
#include <PN532_I2C.h>
#include <PN532.h>
#include <NfcAdapter.h>
#include <Ethernet.h>
#include <PZEM004Tv30.h>

String dev_id = "DEV: 514051";
String tmp_res = "";
bool nfc_enable = true;
bool pzem_enable = false;
bool conn = false;

PN532_I2C pn532i2c(Wire);
PN532 nfc(pn532i2c);

byte server[] = { 192, 168, 1, 6 }; // SMFGC
IPAddress ip(192, 168, 1, 51);
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xEC };
int port = 2316;
EthernetClient client;

PZEM004Tv30 pzem(3, 2); // Software Serial pin 2 (RX) & 3 (TX)
int r1pin = 5;
int r2pin = 6;
int nfcledpin = 8;
int connpin = 7;
int reset = 9;

unsigned long lasttag = 0; // last time you connected to the server, in milliseconds
const unsigned long tagint = 10*1000; // delay between tap, in milliseconds

void setup() {

  pinMode(r1pin, OUTPUT); //pin control relay1
  pinMode(r2pin, OUTPUT); //pin control relay2
  pinMode(nfcledpin, OUTPUT); //pin control nfc
  pinMode(connpin, OUTPUT); //pin control server connection
  pinMode(reset, OUTPUT); //pin control reset system

  digitalWrite(connpin, LOW);
  
  Serial.begin(115200);
  
  Ethernet.begin(mac, ip);
  // Check for Ethernet hardware present
  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    Serial.println(F("Ethernet shield was not found. :("));
    while (true) {
      delay(1000); // do nothing, no point running without Ethernet hardware
    }
  }
  
  Serial.println(F("Initializing NDEF Reader..."));
  nfc.begin();

  uint32_t versiondata = nfc.getFirmwareVersion();
  if (!versiondata) {
    Serial.print(F("Didn't find PN53x board"));
     while (true) {
      delay(1000); // do nothing, no point running without Ethernet hardware
    }
  }
  
  // Got ok data, print it out!
//  Serial.print("Found chip PN5"); Serial.println((versiondata>>24) & 0xFF, HEX); 
//  Serial.print("Firmware ver. "); Serial.print((versiondata>>16) & 0xFF, DEC); 
//  Serial.print('.'); Serial.println((versiondata>>8) & 0xFF, DEC);
  
  // Set the max number of retry attempts to read from a card
  // This prevents us from waiting forever for a card, which is
  // the default behaviour of the PN532.
  nfc.setPassiveActivationRetries(0xFF);
  
  // configure board to read RFID tags
  nfc.SAMConfig();
  
  Serial.println(F("Connecting to server"));
  digitalWrite(nfcledpin, HIGH);
}

void loop() {
  // check if connected
  clientConnect();

  // when the client sends the first byte, say hello:
  if (conn) {
    if (nfc_enable) {
      // send uid tag to server
      uint8_t uid[] = { 0, 0, 0, 0, 0, 0, 0 };
      uint8_t uidLength;
    
      if (nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, &uid[0], &uidLength, 50)) {
        
        tmp_res = ",UID: ";
        for (uint8_t i = 0; i < uidLength; i++) {
          tmp_res.concat(String(uid[i], HEX));
        }

        client.print(dev_id + tmp_res);
        Serial.print(tmp_res);
        Serial.println(F(" -> Sent!"));  

        lasttag = millis();
        digitalWrite(nfcledpin, LOW);
        nfc_enable = false;
      }
      delay(500);
        
    } else {
      // if seconds have passed since your last tap,
      // then allow nfc:
      if (millis() - lasttag > tagint) {
        // NFC LED HERE! 
        digitalWrite(nfcledpin, HIGH); 
        nfc_enable = true;  
      }      
    }
    
    if (client.available() > 0) {
      // get the relay commands from server
      cmd(client.read());
      delay(500);
    }

    if (pzem_enable) {
      tmp_res = ",PZM: ";
  
      float val = pzem.voltage();
      if (!isnan(val)) {
        tmp_res.concat(String(val));
      } else {
        tmp_res.concat("NaN");
      }
    
      val = pzem.current();
      if (!isnan(val)) {
        tmp_res.concat("-" + String(val));
      } else {
        tmp_res.concat("-NaN");
      }
    
      val = pzem.power();
      if (!isnan(val)) {
        tmp_res.concat("-" + String(val));
      } else {
        tmp_res.concat("-NaN");
      }
    
      val = pzem.energy();
      if (!isnan(val)) {
        tmp_res.concat("-" + String(val, 3));
      } else {
        tmp_res.concat("-NaN");
      }
    
      val = pzem.frequency();
      if (!isnan(val)) {
        tmp_res.concat("-" + String(val, 1));
      } else {
        tmp_res.concat("-NaN");
      }
    
      val = pzem.pf();
      if (!isnan(val)) {
        tmp_res.concat("-" + String(val));
      } else {
        tmp_res.concat("-NaN");
      }

      client.print(dev_id + tmp_res);
      delay(2000);
    }
  }
  delay(1); // to lighten server load
}

void clientConnect() {
  // if the server's disconnected, reconnect the client:
  if (conn) {
    if (!client.connected()) {
      conn = false;
      
      digitalWrite(connpin, LOW);
      digitalWrite(nfcledpin, HIGH);
      Serial.println(F("Server closed."));
      client.stop();
      
      delay(1000);
      
      Serial.print(F("Attempting to reconnect"));
    } 
  } 
  else {
    if (client.connect(server, port)) {
      conn = true;
      
      digitalWrite(connpin, HIGH);
      Serial.println(F("-> Connected!"));
      
      tmp_res.concat(dev_id);
      tmp_res.concat(",MAC:");
      tmp_res = String((char*)mac);
      client.println(tmp_res);
      
    } else {
      Serial.print(F("."));
      delay(5000);
    }    
  }
}

void cmd(char data) {
  switch (data) {
    
    case (char)'a':
      digitalWrite(r1pin, HIGH);
      digitalWrite(r2pin, LOW);
      pzem_enable = true;
      break;
  
    case (char)'b':
      digitalWrite(r1pin, LOW);
      digitalWrite(r2pin, HIGH);
      pzem_enable = true;
      break;
  
    case (char)'c':
      digitalWrite(r1pin, HIGH);
      digitalWrite(r2pin, HIGH);
      pzem_enable = true;
      break;
  
    case (char)'d':
      digitalWrite(r1pin, LOW);
      digitalWrite(r2pin, LOW);
      pzem_enable = false;
      Serial.println(F("Logged out!"));
      break;

    case (char)'e':
      Serial.println(F("Logged in!"));
      break;

    case (char)'f':
      digitalWrite(connpin, LOW); //ON
      delay(1000);
      digitalWrite(connpin, HIGH); //OFF
      break;

    case (char)'g':
      for (int i = 0; i < 10; i++) {
        digitalWrite(nfcledpin, LOW); //ON
        delay(100);
        digitalWrite(nfcledpin, HIGH); //OFF
        delay(100);
      }
      break;
  
    case (char)'r':
      pzem.resetEnergy();
      delay(1000);
      digitalWrite(reset, HIGH);
      delay(100);
      digitalWrite(reset, LOW);
      break;
  
    default:
      Serial.println(F("unknown server command."));
      for (int i = 0; i < 10; i++) {
        digitalWrite(connpin, LOW); //ON
        delay(100);
        digitalWrite(connpin, HIGH); //OFF
        delay(100);
      }
      break;
  }
}
