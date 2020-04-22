//This Code Assumes Arduino Uno with the Button connected to port:2
const byte numChars = 64;
char receivedChars[numChars];

boolean newData = false;
const int buttonPin = 2;
byte ledPin = 13;   // the onboard LED
int buttonState = 0;

//===============

void setup() {
    Serial.begin(9600);

    pinMode(buttonPin, INPUT);
    pinMode(ledPin, OUTPUT);
    digitalWrite(ledPin, HIGH);
    delay(200);
    digitalWrite(ledPin, LOW);
    

    Serial.println("<Arduino is ready>");
    Serial.flush();
}

//===============

void loop() {
    receive_start_signal();
    reply_with_results();
}

//===============
void receive_start_signal() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;

    while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();

        if (recvInProgress == true) {
            if (rc != endMarker) {
                receivedChars[ndx] = rc;
                ndx++;
                if (ndx >= numChars) {
                    ndx = numChars - 1;
                }
            }
            else {
                receivedChars[ndx] = '\0'; // terminate the string
                recvInProgress = false;
                ndx = 0;
                newData = true;
            }
        }

        else if (rc == startMarker) {
            recvInProgress = true;
        }
    }
}

void reply_with_results() {
char rc;
rc = Serial.read();
if(rc == "L1"){
  digitalWrite(ledPin, HIGH);
}else if(rc == "L0"){
  digitalWrite(ledPin, LOW);
}
  
  /*
    if (newData == true) {
      buttonState = 0;
      delay(1000);
      delay(random(1000,10000));
        int start_time = (millis());
        int end_time;
        //If button is pressed, take end time
        digitalWrite(ledPin, HIGH);
        while(!buttonState) {
            buttonState = digitalRead(buttonPin);
            //Serial.print(millis());
            //Serial.print('\n');
          }
        end_time = (millis());
        int score = end_time - start_time;
        Serial.print("<Test Result: ");
        Serial.print(score);
        Serial.print(" ms>");
        Serial.print('\n');
        Serial.flush();
            // change the state of the LED everytime a reply is sent to show that it got the message and then responded to the pc
        digitalWrite(ledPin, ! digitalRead(ledPin));
        newData = false;
    }
    */
}
