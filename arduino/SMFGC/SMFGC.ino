#include <SPI.h>
#include <Wire.h>
#include <PN532_I2C.h>
#include <PN532.h>
#include <NfcAdapter.h>
#include <Ethernet.h>
#include <PZEM004Tv30.h>

String dev_id = "DEV:415051";
String m_uid = "79eb5a59";
String tmp_res = "";

bool nfc_enable = false;
bool pzem_enable = false;
bool conn = false;
bool login = true;

PN532_I2C pn532i2c(Wire);
PN532 nfc(pn532i2c);

byte server[] = { 192, 168, 0, 5 }; // SMFGC Server
byte mac[] = { 0xDE, 0xAD, 0xBE, 0x06, 0x55, 0x4B };
EthernetClient client;

PZEM004Tv30 pzem(3, 2); // Software Serial pin 2 (RX) & 3 (TX)
int r1pin = 5;
int r2pin = 6;
int nfcledpin = 8;
int connledpin = 7;
int reset = 9;

float tmp_val; // pzem value storage
unsigned long lasttag = 0; // last time you tag
const unsigned long tagint = 10*1000; // delay after tag

void setup() {

  pinMode(r1pin, OUTPUT); //pin control relay1
  pinMode(r2pin, OUTPUT); //pin control relay2
  pinMode(nfcledpin, OUTPUT); //nfc led status
  pinMode(connledpin, OUTPUT); //server connection led status
  pinMode(reset, OUTPUT); //pin control reset system

  digitalWrite(connledpin, LOW);
  
  Serial.begin(115200);
  Serial.println(dev_id);
  
  // Check for Ethernet hardware present
  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    Serial.println(F("Ethernet shield was not found. :("));
    while (true) {
      delay(1000); // do nothing, no point running without Ethernet hardware
    }
  }
  Ethernet.begin(mac);
  
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
  // Serial.print("Found chip PN5"); Serial.println((versiondata>>24) & 0xFF, HEX); 
  // Serial.print("Firmware ver. "); Serial.print((versiondata>>16) & 0xFF, DEC); 
  // Serial.print('.'); Serial.println((versiondata>>8) & 0xFF, DEC);
  
  // Set the max number of retry attempts to read from a card
  // This prevents us from waiting forever for a card, which is
  // the default behaviour of the PN532.
  nfc.setPassiveActivationRetries(0xFF);
  
  // configure board to read RFID tags
  nfc.SAMConfig();
  
  Serial.println(F("Connecting to server"));
}

void loop() {
  // if the server's disconnected, reconnect the client:
  if (conn) {
    if (!client.connected()) {
      conn = false;      
      digitalWrite(connledpin, LOW);
      Serial.println(F("Server closed."));
      client.stop();
      delay(500);
      
      Serial.print(F("Attempting to reconnect"));
    } 
  } 
  else {
    Ethernet.maintain();
    Serial.println(Ethernet.linkStatus());
    if (client.connect(server, 2316)) {
      conn = true;
      digitalWrite(connledpin, HIGH);
      Serial.println(F("-> Connected!"));
      client.println(dev_id);      
      
    } else {
      Serial.print(F("."));
      delay(3000);
    }    
  }

  if (nfc_enable) {
    uint8_t uid[] = { 0, 0, 0, 0, 0, 0, 0 };
    uint8_t uidLength;
  
    if (nfc.readPassiveTargetID(PN532_MIFARE_ISO14443A, &uid[0], &uidLength, 50)) {
      tmp_res = ",UID: ";
      for (uint8_t i = 0; i < uidLength; i++) {
        tmp_res.concat(String(uid[i], HEX));
      }

      if (tmp_res.indexOf(m_uid) > 0) {
        if (login) {
          cmd('c');
          login = false;
        } else {
          cmd('d');
          login = true;
        }
        Serial.println(F("Master Key Found!"));  
      }
      
      // if connected send uidtag to server.
      if (conn) {
        client.print(dev_id + tmp_res);
        Serial.print(tmp_res);
        Serial.println(F(" -> Sent!"));          
      }

      lasttag = millis();
      digitalWrite(nfcledpin, LOW);
      nfc_enable = false;
      delay(100);
    }   
  } else {
    // if seconds have passed since your last tap, then allow nfc:
    if (millis() - lasttag > tagint) {
      digitalWrite(nfcledpin, HIGH); 
      nfc_enable = true;  
    }      
  }
  
  if (conn && client.available() > 0) {
    // get the commands from server
    cmd(client.read());
    delay(100);
  }
  
  if (conn && pzem_enable) {
    tmp_res = ",PZM: ";

    tmp_val = pzem.voltage();
    if (!isnan(tmp_val)) {
      tmp_res.concat(String(tmp_val));
    } else {
      tmp_res.concat("NaN");
    }
  
    tmp_val = pzem.current();
    if (!isnan(tmp_val)) {
      tmp_res.concat("-" + String(tmp_val));
    } else {
      tmp_res.concat("-NaN");
    }
  
    tmp_val = pzem.power();
    if (!isnan(tmp_val)) {
      tmp_res.concat("-" + String(tmp_val));
    } else {
      tmp_res.concat("-NaN");
    }
  
    tmp_val = pzem.energy();
    if (!isnan(tmp_val)) {
      tmp_res.concat("-" + String(tmp_val, 3));
    } else {
      tmp_res.concat("-NaN");
    }
  
    tmp_val = pzem.frequency();
    if (!isnan(tmp_val)) {
      tmp_res.concat("-" + String(tmp_val, 1));
    } else {
      tmp_res.concat("-NaN");
    }
  
    tmp_val = pzem.pf();
    if (!isnan(tmp_val)) {
      tmp_res.concat("-" + String(tmp_val));
    } else {
      tmp_res.concat("-NaN");
    }

    client.print(dev_id + tmp_res);
    delay(1000);
  }
    
  delay(1); // to lighten server load
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
      break;

    case (char)'e':
      Serial.println(F("Logged in!"));
      break;

    case (char)'f':
      digitalWrite(connledpin, LOW); //ON
      delay(1000);
      digitalWrite(connledpin, HIGH); //OFF
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
      for (int i = 0; i < 10; i++) {
        digitalWrite(connledpin, LOW); //ON
        delay(100);
        digitalWrite(connledpin, HIGH); //OFF
        delay(100);
      }
      break;
  }
}
