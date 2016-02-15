/*
    4/27/2009
    Copyright Spark Fun Electronics© 2008
    By Ryan Owens
    
    ATmega168
    
    Uses RGB matrix backpack v2 (v24.brd)
	Takes in 64 bytes of data

*/

#include <inttypes.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include "6x8.h"

/*********************************************************
						Global MACROS
*********************************************************/
#define sbi(var, mask)   ((var) |= (uint8_t)(1 << mask))
#define cbi(var, mask)   ((var) &= (uint8_t)~(1 << mask))

/*********************************************************
						I/O Definitions
*********************************************************/
//Port C Definitions
#define CLK		0
#define DATA	1
#define LATCH	2
#define CLR		3
#define EN		4

//Port B Definitions
#define CS		2
#define MOSI	3
#define MISO	4
#define SCK		5

#define SUBCOMMANDS 17

#define SUBCOMMAND_SETALL 0
#define SUBCOMMAND_SETNUMBOARDS 1

#define SUBCOMMAND_SCROLLX 2
#define SUBCOMMAND_SCROLLY 3

#define SUBCOMMAND_SETFORECOLOR 4
#define SUBCOMMAND_SETBACKCOLOR 5

/// COMMANDS USING FORECOLOR / BACKCOLOR
#define SUBCOMMAND_CLEAR 6       // PARMS: -
#define SUBCOMMAND_DRAWPOINT 7   //        int x, int y
#define SUBCOMMAND_DRAWCHAR 8    //        int x, int y, char c
#define SUBCOMMAND_DRAWROW 9     //        uint row, uint data
#define SUBCOMMAND_DRAWCOL 10    //        uint col, uint data
#define SUBCOMMAND_DRAWALL 11    //        uint[8] data


#define SUBCOMMAND_SHIFTLEFT 12  // uint[3] color
#define SUBCOMMAND_SHIFTRIGHT 13 // uint[3] color

#define SUBCOMMAND_DRAWLINEV  14 // uint x uint[3] color
#define SUBCOMMAND_DRAWLINEH  15 // uint y uint[3] color

#define SUBCOMMAND_CLEARPOINT 16 // int x, int y

#define COLOR_CHANNELS 3
#define COLOR_CHANNEL_RED 0
#define COLOR_CHANNEL_GREEN 1
#define COLOR_CHANNEL_BLUE 2

uint8_t backColor[COLOR_CHANNELS];
uint8_t foreColor[COLOR_CHANNELS];

#define ROWS 8
#define COLS 8

uint8_t pointBuffer[ROWS];

/*********************************************************
					  General Definitions
*********************************************************/
//#define NUM_BOARDS 	1	//Defines how many RGB matrices are connected in the sysem. Change this value and recompile if you are using more than one matrix
#define RUN_COUNT_ADDRESS	0
#define NUM_BOARDS_ADDRESS	1	//EEPROM location of "NUM_BOARDS" parameters
#define NUM_LEDS	64	//Defines how many LEDs are on each matix (Shouldn't be changed)

/*********************************************************
					  Global Variables
*********************************************************/
volatile uint8_t red_frame[64];		//Each byte in the buffer represents the brightness of the corresponding Red LED on the matrix
volatile uint8_t green_frame[64];	//Each byte in the buffer represents the brightness of the corresponding GREEN LED on the matrix
volatile uint8_t blue_frame[64];	//Each byte in the buffer represents the brightness of the corresponding BLUE LED on the matrix

volatile uint8_t buffer[NUM_LEDS * 2];		//Temporary Buffer that holds the incoming data from the SPI Bus. This data is parsed into the Red,Green and Blue Frame Buffers
volatile unsigned char frame_index = 0;	//Keeps track of the current index

volatile uint16_t byte_count;		//Counts how many bytes have been received on the SPI bus
volatile uint8_t new_frame;			//Flag telling the main firmware that enough data has been received and a new image can be displayed onto the matrix

volatile uint8_t timer_clicks=0;	//Used for PWM to generate different color brightnesses

volatile uint8_t NUM_BOARDS=0, RUN_COUNT=0;
volatile char command_mode=0;
volatile char value=0;

const char test_frame[]= {			//Used to test the LED. Only accessed in the splash_screen() function;
  3, 3, 3, 3, 3, 3, 3, 3,
  3, 0, 1, 0, 0, 1, 0, 3,
  3, 0, 0, 0, 0, 0, 0, 3,
  3, 0, 0, 0, 0, 0, 0, 3,
  3, 0, 0, 0, 0, 0, 0, 3,
  3, 2, 0, 0, 0, 0, 2, 3,
  3, 0, 2, 2, 2, 2, 0, 3,
  3, 3, 3, 3, 3, 3, 3, 3,
};

volatile uint8_t process_command_now = 0;
volatile uint8_t bytes_expected = 0;
volatile uint8_t current_subcommand = 0;



/*********************************************************
				Function Prototypes
*********************************************************/
/*
Usage:		ioinit();
Purpose:	Initialize I/O, Setup SPI Hardware
Parameters:	None
Return:		None
*/
void ioinit (void);
/*
Usage:		splash_screen();
Purpose:	Tests all of the LEDs. Function will cause matrix to go all Red for 300ms, then all Green for 300ms, then all Blue for 300ms, then display a "smiley"
Parameters:	None
Return:		None
*/
void splash_screen(void);
/*
Usage:		parse_frame();
Purpose:	Parses the red, green and blue values out of the temporary frame buffer and into separate color buffers
Parameters:	None. Uses global buffer[] and copies data to global red_frame[], green_frame[] and blue_frame[]
Return:		None.
*/
void parse_frame555(void);
/*
Usage:		post_frames();
Purpose:	Puts the current image located in the red_frame, green_frame and blue_frame buffers onto the RGB matrix
Parameters:	None
Return:		None
*/
void post_frames(void);
/*
Usage:		shift_out_line(1);
Purpose:	Sends a single row of colors to the RGB matrix.
Parameters:	input row_num - Designates which row will be sent to the matrix
Return:		None
*/
void shift_out_line(volatile uint8_t row_num);
/*
Usage:		delay_us(250);
Purpose:	Delays the firmware for ~1us
Parameters:	input x - number of microseconds to delay
Return:		None
*/
void delay_us(uint8_t x);
/*
Usage:		delay_ms(10);
Purpose:	Delays the firmware for ~1ms
Parameters:	input x - number of ms to delay
Return:		None
*/
void delay_ms(uint16_t x);
/*
Description: Reads the EEPROM data at "Address" and returns the character
Pre: Unsigned Int Address is the address to be read
Post: Character at "Address" is returned
Usage: 	unsigned char Data;
		Data=read_from_EEPROM(0);
*/
unsigned char read_from_EEPROM(unsigned int Address);
/*
Description: Writes an unsigned char(Data)  to the EEPROM at the given Address
Pre: Unsigned Int Address contains address to be written to
	 Unsigned Char Data contains data to be written
Post: EEPROM "Address" contains the "Data"
Usage: write_to_EEPROM(0, 'A');
*/
void write_to_EEPROM(unsigned int Address, unsigned char Data);


void set_all(uint8_t r, uint8_t g, uint8_t b);

#define FONT_WIDTH 6
#define FONT_HEIGHT 8

void draw_character(int8_t x, int8_t y, char c);

/*********************************************************
			Interrupt Service Routines
*********************************************************/
/*
Usage:		Automatic as long as Global Interrupts are enabled (sei())
Purpose:	Initialize I/O, Setup SPI Hardware
Parameters:	None
Return:		None
*/
ISR (SPI_STC_vect) 
{
	cli();	//Halt Interrupts

	value=SPDR;	//Get the data from the SPI bus and put it into the temporary frame buffer

	if(!command_mode && value != '*'){
		buffer[frame_index] = value;
		
		frame_index = ((frame_index + 1) & 0x7F) ;	//Frame index counts from 0-63 then wraps back to 0
		SPDR = buffer[frame_index];	//Pass the data along to the next matrix in line.
		
		byte_count++;	//Keep track o f how many bytes we receive

		//If we've received enough data for all of the boards currently connected, tell the main firmware we can post a new frame!
		if(byte_count == NUM_LEDS*2){
			new_frame = 1;	//Post a new frame when we receive enough bytes for all of the LEDS in the 'system'
			byte_count=0;	//Reset the byte count
		}
	}

	if (command_mode) {

	   if (byte_count == 1) {
           
		   if (value < SUBCOMMANDS) {

		    current_subcommand = value;

		   	if (current_subcommand == SUBCOMMAND_SETALL) { // Buffer
		
				bytes_expected = 4;

			} else if (current_subcommand == SUBCOMMAND_SETNUMBOARDS) { // Stuff

			    bytes_expected = 2;

			} else if (current_subcommand == SUBCOMMAND_SCROLLX) { // Draw

			    bytes_expected = 2;

			} else if (current_subcommand == SUBCOMMAND_SCROLLY) { // Draw

			    bytes_expected = 2;

			} else if (current_subcommand == SUBCOMMAND_SETFORECOLOR) { // Draw

			    bytes_expected = 4;

			} else if (current_subcommand == SUBCOMMAND_SETBACKCOLOR) { // Draw

			    bytes_expected = 4;

			} else if (current_subcommand == SUBCOMMAND_CLEAR) { // Draw

			    bytes_expected = 2;

			} else if (current_subcommand == SUBCOMMAND_DRAWPOINT) { // Draw

			    bytes_expected = 3;

			} else if (current_subcommand == SUBCOMMAND_DRAWCHAR) { // Draw

			    bytes_expected = 4;

			} else if (current_subcommand == SUBCOMMAND_DRAWROW) { // Draw

			    bytes_expected = 3;

			} else if (current_subcommand == SUBCOMMAND_DRAWCOL) {  // Draw

			    bytes_expected = 3;

			} else if (current_subcommand == SUBCOMMAND_DRAWALL) { // Draw

			    bytes_expected = 10;

			} else if (current_subcommand == SUBCOMMAND_SHIFTLEFT) { // Buffer

			    bytes_expected = 4;

			} else if (current_subcommand == SUBCOMMAND_SHIFTRIGHT) { // Buffer

			    bytes_expected = 4;

			} else if (current_subcommand == SUBCOMMAND_DRAWLINEV) { // Buffer

			    bytes_expected = 5;

			} else if (current_subcommand == SUBCOMMAND_DRAWLINEH) { // Buffer

			    bytes_expected = 5;

			} else if (current_subcommand == SUBCOMMAND_CLEARPOINT) { // Draw

			    bytes_expected = 3;
			}

		   }

	   } else {

	   		if (!bytes_expected) {


					// Not a valid command. Pack things up.
					process_command_now = 0;

					command_mode = 0;

					byte_count = 0;

					bytes_expected = 0;


			} else {

					// Write bytes to buffer
					buffer[byte_count] = value;

			   		if (byte_count == bytes_expected) {

						process_command_now = 1;

						command_mode = 0;

						byte_count = 0;

						bytes_expected = 0;

					}

			}

	   }

	   byte_count++;

	}

	if ((value == '*') && (byte_count == 0)) {

	    buffer[byte_count] = value;

		command_mode=1;

		bytes_expected = 0;

		byte_count++;

	}


	sei();
}

/*********************************************************
			Buffer Update Routines
*********************************************************/

void update_singlepoint(uint8_t x, uint8_t y)
{
	int bufferPosition;
	
	bufferPosition = (y * COLS) + x;

	if (pointBuffer[y] & (1 << x)) {

		red_frame[bufferPosition] = foreColor[COLOR_CHANNEL_RED];
		green_frame[bufferPosition] = foreColor[COLOR_CHANNEL_GREEN];
		blue_frame[bufferPosition] = foreColor[COLOR_CHANNEL_BLUE];

	} else {

		red_frame[bufferPosition] = backColor[COLOR_CHANNEL_RED];
		green_frame[bufferPosition] = backColor[COLOR_CHANNEL_GREEN];
		blue_frame[bufferPosition] = backColor[COLOR_CHANNEL_BLUE];

	}
}

void update_buffer_row(uint8_t y)
{

	int bufferPosition;
	
	for (int i = 0; i < COLS; i++) {

		bufferPosition = (y * COLS) + i;

		if (pointBuffer[y] & (1 << i)) {
						
			red_frame[bufferPosition] = foreColor[COLOR_CHANNEL_RED];
			green_frame[bufferPosition] = foreColor[COLOR_CHANNEL_GREEN];
			blue_frame[bufferPosition] = foreColor[COLOR_CHANNEL_BLUE];

		} else {

			red_frame[bufferPosition] = backColor[COLOR_CHANNEL_RED];
			green_frame[bufferPosition] = backColor[COLOR_CHANNEL_GREEN];
			blue_frame[bufferPosition] = backColor[COLOR_CHANNEL_BLUE];

		}

	}

}

void update_buffer()
{

	for (int i = 0; i < ROWS; i++) update_buffer_row(i);

}

void draw_clear()
{
	
	for (int i = 0; i < ROWS; i++) pointBuffer[i] = 0;

}


void shift_right(uint8_t r, uint8_t g, uint8_t b)
{

	for (int i = 62; i > 0; i--)
	{
		red_frame[i + 1] = red_frame[i];
		green_frame[i + 1] = green_frame[i];
		blue_frame[i + 1] = blue_frame[i];
	}

	red_frame[0] = r;
	green_frame[0] = g;
	blue_frame[0] = b;


}

void shift_left(uint8_t r, uint8_t g, uint8_t b)
{

	for (int i = 0; i < 63; i++)
	{
		red_frame[i] = red_frame[i + 1];
		green_frame[i] = green_frame[i + 1];
		blue_frame[i] = blue_frame[i + 1];
	}

	red_frame[63] = r;
	green_frame[63] = g;
	blue_frame[63] = b;

}


/*********************************************************
			Drawing Routines
*********************************************************/
/*
void draw_character(int8_t x, int8_t y, char character)
{

	uint8_t * characterptr = (uint8_t *)f6x8;
	
	characterptr += 8 * (uint8_t)character;

	for (uint8_t iy = y; iy < 8; iy++) 
	{
		pointBuffer[iy] = 0;

		for (uint8_t ix = 0; ix < 8; ix++)
		{
			
			if (*characterptr & (1 << ix)) pointBuffer[iy] |= (1 << (ix + x)); 

		}

		characterptr += 1;

	}
}
*/

void draw_character(int8_t x, int8_t y, char character)
{

    if (x > 7) return;
	if (y > 7) return;
	if (x < -7) return;
	if (y < -7) return;

	uint8_t * characterptr = (uint8_t *)f6x8;
	
	characterptr += 8 * (uint8_t)character;

    int8_t startcol = x;

    int8_t startrow = y;

	if (x < 0) {

		startcol = (int8_t)(-x);

		x = 0;

	} else {

	    startcol = 0;

	}

	if (y < 0) {

		startrow = (int8_t)(-y);

		y = 0;

	} else {

		startrow = 0;

	}


    characterptr += startrow;
    
	for (uint8_t iy = y; iy < (8 - startrow - y); iy++) 
	{
		// pointBuffer[iy] = 0;

		int8_t dotctr = startcol;

		for (uint8_t ix = x; ix < (8 - startcol); ix++)
		{
			
			if (*characterptr & (1 << dotctr)) pointBuffer[iy] |= (1 << ix); 

			dotctr++;

		}

		characterptr += 1;

	}
}

void draw_line_v(uint8_t x, uint8_t r, uint8_t g, uint8_t b)
{
	for (uint8_t y = 0; y < 8; y++) 
	{

		uint8_t index = (y * 8) + x;

		red_frame[index] = r;
		green_frame[index] = g;
		blue_frame[index] = b;

	}

}


void draw_line_h(uint8_t y, uint8_t r, uint8_t g, uint8_t b)
{

	for (int x = 0; x < 8; x++) 
	{

		uint8_t index = (y * 8) + x;

		red_frame[index] = r;
		green_frame[index] = g;
		blue_frame[index] = b;

	}

}



/*********************************************************
			Command Process Routines
*********************************************************/
/*
Usage:		Execute commands received and verified by ISR 
Purpose:	
Parameters:	None
Return:		None
*/
void process_command() 
{

	switch(current_subcommand) {

		case SUBCOMMAND_SETALL:

				set_all(buffer[2], buffer[3], buffer[4]);

			break;

		case SUBCOMMAND_SETNUMBOARDS:

				if(buffer[2] > 0 && buffer[2] < 9) {

					NUM_BOARDS = buffer[2];

					write_to_EEPROM(NUM_BOARDS_ADDRESS, NUM_BOARDS);

				}

			break;

		case SUBCOMMAND_SCROLLX:	 //		int deltax

				update_buffer();
			break;

		case SUBCOMMAND_SCROLLY:     //		int deltay
				update_buffer();
			break;


		case SUBCOMMAND_SETFORECOLOR: //    	uint[3] color
				foreColor[COLOR_CHANNEL_RED]   = buffer[2];
				foreColor[COLOR_CHANNEL_GREEN] = buffer[3];
				foreColor[COLOR_CHANNEL_BLUE]  = buffer[4];
				// update_buffer();
			break;

		case SUBCOMMAND_SETBACKCOLOR: //    	uint[3] color
				backColor[COLOR_CHANNEL_RED]   = buffer[2];
				backColor[COLOR_CHANNEL_GREEN] = buffer[3];
				backColor[COLOR_CHANNEL_BLUE]  = buffer[4];
				// update_buffer();
			break;



		case SUBCOMMAND_CLEAR:       // PARMS: -
				draw_clear();
				update_buffer();
			break;

		case SUBCOMMAND_DRAWPOINT:   //        int x, int y
		        if ((buffer[2] < COLS) && (buffer[3] < ROWS)) {
				    pointBuffer[buffer[3]] |= (1 << buffer[2]);
					update_buffer_row(buffer[3]);
				}
			break;

		case SUBCOMMAND_CLEARPOINT:   //        int x, int y
		        if ((buffer[2] < COLS) && (buffer[3] < ROWS)) {
				    pointBuffer[buffer[3]] &= ~(1 << buffer[2]);
					update_buffer_row(buffer[3]);
				}
			break;

		case SUBCOMMAND_DRAWCHAR:    //        int x, int y, char c

		        if ((buffer[4] > 31) && (buffer[4] < 128)) {

                    draw_character(buffer[2], buffer[3], buffer[4]);
					update_buffer();

				}

			break;

		case SUBCOMMAND_DRAWROW:     //        uint row, uint data
//				update_buffer_row(x);
			break;

		case SUBCOMMAND_DRAWCOL:    //        uint col, uint data
				update_buffer();
			break;

		case SUBCOMMAND_DRAWALL:    //        uint[8] data
				update_buffer();
			break;

		case SUBCOMMAND_SHIFTLEFT:    //        uint[3] color
				shift_left(buffer[2], buffer[3], buffer[4]);
			break;

		case SUBCOMMAND_SHIFTRIGHT:    //        uint[3] color
				shift_right(buffer[2], buffer[3], buffer[4]);
			break;

        case SUBCOMMAND_DRAWLINEV:
				draw_line_v(buffer[2], buffer[3], buffer[4],  buffer[5]);
			break;

        case SUBCOMMAND_DRAWLINEH:
				draw_line_h(buffer[2], buffer[3], buffer[4],  buffer[5]);
			break;

		default:
			break;

	}

}

/*********************************************************
						Main Code
*********************************************************/
int main (void)
{
	ioinit ();
	
	//Make sure all the pixels are working
	// if(RUN_COUNT < 10)	
	splash_screen();

	/*
	//Show the "Ready State"
	red_frame[NUM_BOARDS-1] = 31;
	red_frame[(NUM_BOARDS-1)+1] = 31;
	green_frame[(NUM_BOARDS-1)+2] = 31;
	green_frame[(NUM_BOARDS-1)+3] = 31;
	blue_frame[(NUM_BOARDS-1)+4] = 31;
	blue_frame[(NUM_BOARDS-1)+5] = 31;
    */

	
	foreColor[COLOR_CHANNEL_RED]   = 0xFF;
	foreColor[COLOR_CHANNEL_GREEN] = 0x00;
	foreColor[COLOR_CHANNEL_BLUE]  = 0x00;

	backColor[COLOR_CHANNEL_RED]   = 0x00;
	backColor[COLOR_CHANNEL_GREEN] = 0x00;
	backColor[COLOR_CHANNEL_BLUE]  = 0x00;

	draw_clear();
	//draw_character(1,1,'Y');
    //draw_character(4,1,'U');

	update_buffer();
	

	//draw_line_v(3,0xFF,0x00,0x00);
	//draw_line_h(3,0x00,0xFF,0x00);
	
	//Enable Global Interrupts
	sei();
	while(1)
	{
        if (PINB & (1<<CS)) //If CS goes high, SPI com is complete
		{
			frame_index = 0;//Reset the frame index

			command_mode = 0;

			byte_count = 0;

			bytes_expected = 0;
		}

		if (process_command_now) {

			process_command();
			
			current_subcommand = 0;
			process_command_now = 0;

		}

		post_frames(); //Update display with current frame data  
		
		//Check to see if there is a new frame to parse
		if (new_frame == 1) parse_frame555();
	
		//Increment clicks to determine LED brightness levels for 5 bit color values
		timer_clicks = (timer_clicks + 1) & 0x1F; //0b00011111; //Circular 0 to 1F
	}
	
	return (0);
}

/*********************************************************
						Functions
*********************************************************/
/*
Usage:		ioinit();
Purpose:	Initialize I/O, Setup SPI Hardware
Parameters:	None
Return:		None
*/
void ioinit (void)
{
	//1 = Output, 0 = Input
	DDRB = (1<<MISO);	//Enable internal pull-up resistor on MISO
	PORTB = (1<<MOSI)|(1<<CS)|(1<<SCK);	//Set SPI Outputs	
	
	DDRC = 0x1F;	//Set outputs to the shift registers
	DDRD = 0xFF;	//All Port D pins are outputs to the Sync Driver

	//Set initial pin states
	sbi(PORTC, CLK);
	sbi(PORTC, CLR);
	sbi(PORTC, DATA);
	sbi(PORTC, LATCH);
	sbi(PORTC, EN); 
	
	//Setup the SPI Hardware
	SPCR = (1<<SPE) | (1<<SPIE); //Enable SPI, Enable SPI Interrupts
	
	//Load the NUM_BOARDS parameter from EEPROM
	RUN_COUNT = read_from_EEPROM(RUN_COUNT_ADDRESS);
	if(RUN_COUNT == 0xFF){
		RUN_COUNT = 1;
		write_to_EEPROM(RUN_COUNT_ADDRESS, RUN_COUNT);
		NUM_BOARDS=1;
		write_to_EEPROM(NUM_BOARDS_ADDRESS, NUM_BOARDS);
	}
	else{
		RUN_COUNT = read_from_EEPROM(RUN_COUNT_ADDRESS);
		if(RUN_COUNT < 10){
			RUN_COUNT+=1;
			write_to_EEPROM(RUN_COUNT_ADDRESS, RUN_COUNT);
		}
		NUM_BOARDS=read_from_EEPROM(NUM_BOARDS_ADDRESS);
		if(NUM_BOARDS > 8)NUM_BOARDS=1;
	}
		
}



void set_all(uint8_t r, uint8_t g, uint8_t b)
{

	uint16_t i;

	for(i = 0 ; i < 64 ; i++)
	{
		red_frame[i] = r;
		green_frame[i] = g;
		blue_frame[i] = b;
	}
	
}


/*
Usage:		splash_screen();
Purpose:	Tests all of the LEDs. Function will cause matrix to go all Red for 300ms, then all Green for 300ms, then all Blue for 300ms, then display a "smiley"
Parameters:	None
Return:		None
*/
void splash_screen(void)
{
	uint16_t i;
	cli();
	//Fill red
	for(i = 0 ; i < 64 ; i++)
	{
		red_frame[i] = 7;
		green_frame[i] = 0;
		blue_frame[i] = 0;
	}

	for(i = 0 ; i < 1000 ; i++)
		post_frames();

	//Fill green
	for(i = 0 ; i < 64 ; i++)
	{
		red_frame[i] = 0;
		green_frame[i] = 7;
		blue_frame[i] = 0;
	}

	for(i = 0 ; i < 1000 ; i++)
		post_frames();

	//Fill blue
	for(i = 0 ; i < 64 ; i++)
	{
		red_frame[i] = 0;
		green_frame[i] = 0;
		blue_frame[i] = 7;
	}

	for(i = 0 ; i < 1000 ; i++)
		 post_frames(); 
		 
    /*
	//Fill Smiley Face
	for(i = 0 ; i < 64 ; i++)
	{
		if(test_frame[i]==1)red_frame[i] = 0x1F;
		else red_frame[i]=0;
		
		if(test_frame[i]==2)green_frame[i] = 0x1F;
		else green_frame[i] = 0;
		
		if(test_frame[i]==3)blue_frame[i] = 0x1F;
		else blue_frame[i]=0;

		red_frame[i]=1;
	}

	for(i = 0 ; i < 3000 ; i++)
		 post_frames(); 		 

	*/

	//Erase frame data
	for(i = 0 ; i < 64 ; i++)
	{
		red_frame[i] = 0;
		green_frame[i] = 0;
		blue_frame[i] = 0;
	}
	sei();
	PORTD = 0; //Turn off display
}

/*
Usage:		parse_frame();
Purpose:	Parses the red, green and blue values out of the temporary frame buffer and into separate color buffers
Parameters:	None. Uses global buffer[] and copies data to global red_frame[], green_frame[] and blue_frame[]
Return:		None.
*/
void parse_frame332(void)
{
    uint8_t color_value;
    
    for(int LED = 0 ; LED < 64 ; LED++)
    {
        color_value = buffer[63-LED];
        red_frame[LED] = (color_value & 0xE0) >> 5;	//(temp & 0b11100000) >> 5; Highes 3 bits represent the Red value for the current LED
        green_frame[LED] = (color_value & 0x1C) >> 2; 	//(temp & 0b00011100) >> 2; Next 3 bits represent the Green value for the current LED
        blue_frame[LED] = (color_value & 0x03); 		//(temp & 0b00000011); Final 2 bits represent the Blue value for the current LED
    }
    new_frame = 0; //Reset new frame flag
}

void parse_frame555(void)
{
    uint8_t color_value1;
	uint8_t color_value2;
    
    for(int LED = 0 ; LED < 64 ; LED++)
    {
        color_value1 = buffer[(LED << 1)];
		color_value2 = buffer[(LED << 1)+1];

		//(temp1 & 0b00011111) >> 0
        red_frame[LED]   = ((color_value1 & 0x1F) >> 0);	

		//(temp1 & 0b11100000) >> 5 | (temp2 & 0b00000011) << 3
        green_frame[LED] = ((color_value1 & 0xE0) >> 5) | ((color_value2 & 0x3) << 3); 	

		// (temp2 & 0b01111100) >> 2
        blue_frame[LED]  = ((color_value2 & 0x7C) >> 2); 		
    }
    new_frame = 0; //Reset new frame flag
}

/*
Usage:		post_frames();
Purpose:	Puts the current image located in the red_frame, green_frame and blue_frame buffers onto the RGB matrix
Parameters:	None
Return:		None
*/
void post_frames(void)
{	
	for(char row = 0 ; row < 8; row++)shift_out_line(row);	//Send all 8 rows of colors to the Matrix
}

/*
Usage:		shift_out_line(1);
Purpose:	Sends a single row of colors to the RGB matrix.
Parameters:	input row_num - Designates which row will be sent to the matrix
Return:		None
*/
void shift_out_line(volatile uint8_t row_num)
{
	cbi(PORTC, LATCH);	//Disable the shift registers

	uint8_t lineStart = row_num << 3;
    uint8_t lineEnd = lineStart + 8;

	//Send Red Values
	//for(uint8_t LED = lineStart ; LED < lineEnd ; LED++) //Step through bits
	//for(uint8_t LED = (lineEnd - 1) ; LED >= lineStart ; LED--) //Step through bits
	for(uint8_t LED = 1 ; LED < 9 ; LED++) //Step through bits
	{
		cbi(PORTC, CLK);	//Lower the shift register clock so we can configure the data
		
		//Compare the current color value to timer_clicks to Pulse Width Modulate the LED to create the designated brightness
		if(timer_clicks < red_frame[lineEnd - LED])
			sbi(PORTC, DATA);
		else
			cbi(PORTC, DATA);

		sbi(PORTC, CLK);	//Raise the shift register clock to lock in the data
	}

	//Send Blue Values
	//for(uint8_t LED = lineStart ; LED < lineEnd ; LED++) //Step through bits
	for(uint8_t LED = 1 ; LED < 9 ; LED++) //Step through bits
	{
		cbi(PORTC, CLK);	//Lower the shift register clock so we can configure the data
	
		//Compare the current color value to timer_clicks to Pulse Width Modulate the LED to create the designated brightness
		if(timer_clicks < blue_frame[lineEnd - LED])
			sbi(PORTC, DATA);
		else
			cbi(PORTC, DATA);

		sbi(PORTC, CLK);	//Raise the shift register clock to lock in the data
	}

	//Send Green Values
	//for(uint8_t LED = lineStart ; LED < lineEnd ; LED++) //Step through bits
	for(uint8_t LED = 1 ; LED < 9 ; LED++) //Step through bits
	{
		cbi(PORTC, CLK);	//Lower the shift register clock so we can configure the data
	
		//Compare the current color value to timer_clicks to Pulse Width Modulate the LED to create the designated brightness
		if(timer_clicks < green_frame[lineEnd - LED])
			sbi(PORTC, DATA);
		else
			cbi(PORTC, DATA);

		sbi(PORTC, CLK);	//Raise the shift register clock to lock in the data
	}

	sbi(PORTC, EN);		//Disable the Shift Register Outputs
	sbi(PORTC, LATCH);	//Put the new data onto the outputs of the shift register

	PORTD = (1<<row_num); //Sink current through row (Turns colors 'ON' for the given row. Keep in mind that we can only display to 1 row at a time.)

	cbi(PORTC, EN);		//Enable the Shift Register Outputs
}

/*
Usage:		delay_us(250);
Purpose:	Delays the firmware for ~1us
Parameters:	input x - number of microseconds to delay
Return:		None
*/
void delay_us(uint8_t x)
{
	TIFR0 = (1<<TOV0); //Clear any interrupt flags on Timer2
	TCNT0 = 256 - x; //256 - 125 = 131 : Preload timer 2 for x clicks. Should be 1us per click
	while( (TIFR0 & (1<<TOV0)) == 0);

	//Double the delay because we are using 16MHz and 8 prescalar

	TIFR0 = (1<<TOV0); //Clear any interrupt flags on Timer2
	TCNT0 = 256 - x; //256 - 125 = 131 : Preload timer 2 for x clicks. Should be 1us per click
	while( (TIFR0 & (1<<TOV0)) == 0);
}

/*
Usage:		delay_ms(10);
Purpose:	Delays the firmware for ~1ms
Parameters:	input x - number of ms to delay
Return:		None
*/
void delay_ms(uint16_t x)
{
	for ( ; x > 0 ; x--)
	{
		delay_us(250);
		delay_us(250);
		delay_us(250);
		delay_us(250);
	}
}

//Description: Writes an unsigned char(Data)  to the EEPROM at the given Address
//Pre: Unsigned Int Address contains address to be written to
//	 Unsigned Char Data contains data to be written
//Post: EEPROM "Address" contains the "Data"
//Usage: write_to_EEPROM(0, 'A');
void write_to_EEPROM(unsigned int Address, unsigned char Data)
{
    //Interrupts are globally disabled!
	
	while(EECR & (1<<EEPE)); //Wait for last Write to complete
	//May need to wait for Flash to complete also!
	EEAR = Address;			//Assign the Address Register with "Address"
	EEDR=Data;				//Put "Data" in the Data Register
	EECR |= (1<<EEMPE); 	//Write to Master Write Enable
	EECR |= (1<<EEPE);  	//Start Write by setting EE Write Enable
	
}

//Description: Reads the EEPROM data at "Address" and returns the character
//Pre: Unsigned Int Address is the address to be read
//Post: Character at "Address" is returned
//Usage: 	unsigned char Data;
//		Data=read_from_EEPROM(0);
unsigned char read_from_EEPROM(unsigned int Address)
{
	//Interrupts are globally disabled!
	
	while(EECR & (1<<EEPE));	//Wait for last Write to complete
	EEAR = Address;				//Assign the Address Register with "Address"
	EECR |= (1<<EERE); 			//Start Read by writing to EER
	return EEDR;				//EEPROM Data is returned
}
