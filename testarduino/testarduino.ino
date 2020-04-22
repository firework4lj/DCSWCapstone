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
}

//===============

void loop() {

    wait_to_start();
    start_running();

}

//===============

void wait_to_start() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;
    boolean started=0;

    while (!started) {
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
            started=1;
        }
    }
    newData=false;
}
//===============

void start_running(){

  boolean going=true;
  while(going){
    send_packet();
    check_for_request();
  }
}

//===============


void send_packet(){
  char packet[8];
  /*
  packet[0]='A';
  packet[1]='A';
  packet[2]='A';
  packet[3]='A';
  packet[4]='A';
  packet[5]='A';
  packet[6]='A';
  packet[7]='\n';
  Serial.print(packet);
  */
}

//===============

void check_for_request(){

    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;
    int l=0;
   while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();
        l++;
            if (rc != '.') {
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
   newData=false;
   Serial.print(receivedChars[0]);
         if(rc == "L0"){
           digitalWrite(ledPin, LOW);
         }
         if(rc == "L1"){
           digitalWrite(ledPin, HIGH);
         }
    if(receivedChars[0]=='V'){
         if(receivedChars[1]=='0'){
           //Vibration OFF
         }
         if(receivedChars[1]=='1'){
           //Vibration ON
         }
    }
    if(receivedChars[0]=='S'){
         if(receivedChars[1]=='0'){
           //Sound OFF
         }
         if(receivedChars[1]=='1'){
           //Sound ON
         }
    }
}

//===============

//===============

void reply_with_results(int results) {
    if (newData == true) {
        Serial.print("<Test Result: ");
        Serial.print(results);
        Serial.print('>');
        Serial.print('\n');
            // change the state of the LED everytime a reply is sent to show that it got the message and then responded to the pc
        digitalWrite(ledPin, ! digitalRead(ledPin));
        newData = false;
    }
}

/*int responseTimer(){
    return 100;
}*/
