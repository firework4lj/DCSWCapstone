const byte numChars = 64;
char receivedChars[numChars];

boolean newData = false;
const int buttonPin = 2;
byte ledPin = 13;   // the onboard LED
byte vibrationpin = 12;
int buttonState = 1;

//===============

void setup() {
    Serial.begin(9600);

    pinMode(buttonPin, INPUT);
    pinMode(ledPin, OUTPUT);
    pinMode(vibrationpin, OUTPUT);
    digitalWrite(ledPin, HIGH);
    digitalWrite(vibrationpin, LOW);
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
 //Button 1/0
 //signed double for accellerometer
 //0-1024 for each pressure sensor
 //
 //          PS0  PS1 B
 // PACKET: XXXX XXXX X
  char button;
  if(digitalRead(11)){
    button='0';
  }
  else{
    button='1';
  }
  int buttonRead;
//  buttonRead=digitalRead(2);
 // char button = buttonRead;
  char packet[10];
  packet[0]='A';
  packet[1]='B';
  packet[2]='C';
  packet[3]='D';
  packet[4]='E';
  packet[5]='F';
  packet[6]='G';
  packet[7]='H';
  packet[8]=button;
  packet[9]='\n';
  delay(50);
  Serial.print(packet);
}

//===============

void check_for_request(){

    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;
    int l=0;
    newData = false;
   
   while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();
        l++;
  //      if (recvInProgress == true) {
            if (rc != '\n') {
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
      //  }
   //     else{
     //      
       //     recvInProgress = true;
        //}
       
    }
    if(receivedChars[0]=='L'){
         if(receivedChars[1]=='0'){
           digitalWrite(ledPin, LOW);
         }
         if(receivedChars[1]=='1'){
           digitalWrite(ledPin, HIGH);
         }
    }
    if(receivedChars[0]=='V'){
         if(receivedChars[1]=='1'){
           digitalWrite(vibrationpin,HIGH);
         }
         if(receivedChars[1]=='0'){
           digitalWrite(vibrationpin,LOW);
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
