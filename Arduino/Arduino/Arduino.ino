const byte numChars = 64;
char receivedChars[numChars]
boolean newData = false;

//set pin values for I/O
const int buttonPin = 2;
byte ledPin = 13;   // the onboard LED
byte vibrationpin = 12;
int buttonState = 1;
const int leftpressurepin = A0;
const int rightpressurepin =  A1;

//===============

void setup() {
    Serial.begin(9600);

    //set up I/O pin modes
    pinMode(buttonPin, INPUT);
    pinMode(ledPin, OUTPUT);
    pinMode(vibrationpin,OUTPUT);
    pinMode(leftpressurepin,INPUT);
    pinMode(rightpressurepin,INPUT);
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

//function waits to recieve start marker then reads in string
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

//constantly send packets to desktop application
//and checking for any requests it sends
void start_running(){

  boolean going=true;
  while(going){
    send_packet();
    check_for_request();
  }
}

//===============


void send_packet(){
 //Packet stored as ArduinoSerialMsg in Home.xaml.cs
 //How input data sent from arduino 
 //Button uses 1/0 for high/low
 //signed double for accellerometer, accellerometer not implimented
 //values are between 0-1024 for each pressure sensor
 //How packet mapped:
 //         PS0  PS1  B
 // PACKET: XXXX XXXX X 

  char button;
  if(digitalRead(11)){
    button='0';
  }
  else{
    button='1';
  }
  int leftsensorint = analogRead(A0);
  int rightsensorint = analogRead(A1);
  int buttonRead;

  //get the ones, tens, hundreds, and thousands place
  // for the value from each sensor
  //This is how the values are sent as 4 chars, one for each place
  char leftsensorchar[4];
  char rightsensorchar[4];
  int tho = 0;
  int hun = 0;
  int ten = 0;
  int one = 0;
  tho = leftsensorint/1000;
  hun = (leftsensorint-(tho*1000))/100;
  ten = (leftsensorint-(tho*1000)-(hun*100))/10;
  one = (leftsensorint-(tho*1000)-(hun*100)-(ten*10));
  tho = tho+48;
  hun = hun+48;
  ten = ten+48;
  one = one+48;
  
  int rtho = 0;
  int rhun = 0;
  int rten = 0;
  int rone = 0;
  if(rightsensorint<1000){
    rtho =0;
  }
  else{
    rtho = 1;
  }
  rhun = (rightsensorint%1000)/100;
  rten = (rightsensorint-(rtho*1000)-(rhun*100))/10;
  rone = (rightsensorint-(rtho*1000)-(rhun*100)-(rten*10));
  rtho = rtho+48;
  rhun = rhun+48;
  rten = rten+48;
  rone = rone+48;

  //Values are put in to packet
  char packet[11] = {tho, hun, ten, one, rtho, rhun, rten, rone, button, '\n'};
  delay(50);
  Serial.print(packet); //this call is what sends the packet
}

//===============
//Checks for requests from monitor app, 
//reads them in, then alters output devices
void check_for_request(){

    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;
    int l=0;
    newData = false;
    
   //read in the request
   while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();
        l++;
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
    }
    //turn on or of LED
    if(receivedChars[0]=='L'){
         if(receivedChars[1]=='0'){
           digitalWrite(ledPin, LOW);
         }
         if(receivedChars[1]=='1'){
           digitalWrite(ledPin, HIGH);
         }
    }
    //turn on or off vibration
    if(receivedChars[0]=='V'){
         if(receivedChars[1]=='1'){
           digitalWrite(vibrationpin,HIGH);
         }
         if(receivedChars[1]=='0'){
          digitalWrite(vibrationpin,LOW);
         }
    }
    //turn on or off sound, sound not currently implimented
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
            // change the state of the LED everytime a reply is sent to show 
            //that it got the message and then responded to the pc
        digitalWrite(ledPin, ! digitalRead(ledPin));
        newData = false;
    }
}
