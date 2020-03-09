/*
SAMD21 free run mode. Source: https://www.youtube.com/watch?v=glulIeL2lxA
To use this, you must edit wiring.c for your board and comment out the lines:

  ADC->CTRLB.reg = ADC_CTRLB_PRESCALER_DIV512 |
                   ADC_CTRLB_RESSEL_10BIT;

  ADC->SAMPCTRL.reg = 0x3f;

On Mac, the wiring.c file can be found at
~/Library/Arduino15/packages/adafruit/hardware/samd/1.2.3/cores/arduino
*/
#include <PacketSerial.h>

#define NSAMPLES 2000

#define PW_PIN 2   //Pin to watch for pulse-width output from rangefinder

//ADC parameters
#define gClk   3   //Which generic clock will we use for the ADC?
#define intPri 0   //Set interrupt priority for ADC
#define cDiv   1   //Divide factor for generic clock

volatile uint32_t count = 0;          //Count number of samples
volatile uint8_t  readbuf[NSAMPLES];  //Hold data from ADC
volatile bool     do_adc = false;     //Set to true to do ADC conversion
volatile uint32_t time, pwtime;

PacketSerial     ps;


void setup()
{
	pinMode(13, OUTPUT);
	pinMode(PW_PIN, INPUT);
	adcPortSetup();
	genericClockSetup(gClk, cDiv);
	adcSetup();
	interruptSetup(intPri);
	adcSWTrigger();  //Start free run mode
	ps.begin(1e6);

	attachInterrupt(digitalPinToInterrupt(PW_PIN), pw_change, CHANGE);
}

void pw_change()
{
	if(digitalRead(PW_PIN) == HIGH)
		time = micros();
	else
		pwtime = micros() - time;
}


void loop()
{
	//Wait for the PW_PIN to go high, then allow the ADC handler to
	// write samples to the readbuf.

	while(digitalRead(PW_PIN) == LOW);

	count = 0;
	do_adc = true;

	//Wait until the ADC handler has read enough samples; after this the
	//ADC handler automatically disables its writing to readbuf
	while(count < NSAMPLES);

	//Maximum PW signal length is 37,338 us; pack this value into the
	// first two bins of the signal since those are too close anyway
	readbuf[0] = (uint8_t)((pwtime >> 8) & 0xff);
	readbuf[1] = (uint8_t)(pwtime & 0xff);

	//Now send the buffer over serial
	ps.send((uint8_t*)readbuf, NSAMPLES);

	//Wait until PW_PIN has gone low again, then return to the top of
	// the loop
	while(digitalRead(PW_PIN) == HIGH);
}


//This ISR is called each time ADC makes a reading (function name defined
// in Arduino code). It writes to readbuf until it reaches NSAMPLES
// then waits for do_adc to be set to true again.
void ADC_Handler()
{
	if(do_adc)
	{
		if(count < NSAMPLES)
			readbuf[count++] = REG_ADC_RESULT;
		else
			do_adc = false;
	}

	//Reset interrupt
	ADC->INTFLAG.reg = ADC_INTENSET_RESRDY;
}


//Configure ADC ports; note this does not use the same pin numbering
// scheme as Arduino
void adcPortSetup()
{
	//ADC input pin (Arduino A0/PA02)
	REG_PORT_DIRCLR1 = PORT_PA02;

	//Enable multiplexing on PA02_AIN0 PA03/ADC_VREFA
	PORT->Group[0].PINCFG[2].bit.PMUXEN = 1;
	PORT->Group[0].PINCFG[3].bit.PMUXEN = 1;
	PORT->Group[0].PMUX[1].reg = PORT_PMUX_PMUXE_B | PORT_PMUX_PMUXO_B;
}


//this function sets up the generic clock that will be used for the
//ADC unit by default it uses the 48M system clock, input arguments
//set divide factor for generic clock and choose which generic clock
//Note unless you understand how the clock system works use clock 3.
//clocks 5 and up can brick the microcontroller based on how Arduino
//configures things
void genericClockSetup(int clk, int dFactor)
{
	// Enable the APBC clock for the ADC
	REG_PM_APBCMASK |= PM_APBCMASK_ADC;

	//This allows you to setup a div factor for the selected clock
	//certain clocks allow certain division factors: Generic clock
	//generators 3 - 8 8 division factor bits - DIV[7:0]
	GCLK->GENDIV.reg |= GCLK_GENDIV_ID(clk) | GCLK_GENDIV_DIV(dFactor);
	while(GCLK->STATUS.reg & GCLK_STATUS_SYNCBUSY);

	//configure the generator of the generic clock with 48MHz clock
	//GCLK->GENCTRL.reg |= GCLK_GENCTRL_GENEN | GCLK_GENCTRL_SRC_DFLL48M | GCLK_GENCTRL_ID(clk);	// GCLK_GENCTRL_DIVSEL don't need this, it makes divide based on power of two
	GCLK->GENCTRL.reg |= GCLK_GENCTRL_GENEN | GCLK_GENCTRL_SRC_OSC8M | GCLK_GENCTRL_ID(clk);	// GCLK_GENCTRL_DIVSEL don't need this, it makes divide based on power of two
	while(GCLK->STATUS.reg & GCLK_STATUS_SYNCBUSY);

	//enable clock, set gen clock number, and ID to where the clock goes
	//(30 is ADC)
	GCLK->CLKCTRL.reg |=
		GCLK_CLKCTRL_CLKEN | GCLK_CLKCTRL_GEN(clk) | GCLK_CLKCTRL_ID(30);
	while(GCLK->STATUS.bit.SYNCBUSY);
}

/*
ADC_CTRLB_PRESCALER_DIV4_Val    0x0u  
ADC_CTRLB_PRESCALER_DIV8_Val    0x1u   
ADC_CTRLB_PRESCALER_DIV16_Val   0x2u   
ADC_CTRLB_PRESCALER_DIV32_Val   0x3u   
ADC_CTRLB_PRESCALER_DIV64_Val   0x4u   
ADC_CTRLB_PRESCALER_DIV128_Val  0x5u   
ADC_CTRLB_PRESCALER_DIV256_Val  0x6u   
ADC_CTRLB_PRESCALER_DIV512_Val  0x7u   
--> 8 bit ADC measurement takes 5 clock cycles, 10 bit ADC measurement
		takes 6 clock cycles
--> Using 48MHz system clock with division factor of 1
--> Using ADC division factor of 32
--> Sample rate = 48M / (5 x 32) = 300 KSPS
This function sets up the ADC, including setting resolution and ADC
sample rate
*/
void adcSetup()
{
	// Select reference
	REG_ADC_REFCTRL = ADC_REFCTRL_REFSEL_INTVCC1;	//set vref for ADC to VCC

	// Average control 1 sample, no right-shift
	REG_ADC_AVGCTRL |= ADC_AVGCTRL_SAMPLENUM_1;

	// Sampling time, no extra sampling half clock-cycles
	REG_ADC_SAMPCTRL = ADC_SAMPCTRL_SAMPLEN(0);

	// Input control and input scan
	REG_ADC_INPUTCTRL |=
		ADC_INPUTCTRL_GAIN_1X | ADC_INPUTCTRL_MUXNEG_GND |
		ADC_INPUTCTRL_MUXPOS_PIN0;
	// Wait for synchronization
	while(REG_ADC_STATUS & ADC_STATUS_SYNCBUSY);

	//This is where you set the divide factor, note that the divide call
	// has no effect until you change Arduino wire.c
	//ADC->CTRLB.reg |= ADC_CTRLB_RESSEL_8BIT | ADC_CTRLB_PRESCALER_DIV32 | ADC_CTRLB_FREERUN;
	ADC->CTRLB.reg |= ADC_CTRLB_RESSEL_8BIT | ADC_CTRLB_PRESCALER_DIV16 | ADC_CTRLB_FREERUN;	//This is where you set the divide factor, note that the divide call has no effect until you change Arduino wire.c
	//Wait for synchronization
	while(REG_ADC_STATUS & ADC_STATUS_SYNCBUSY);

	ADC->WINCTRL.reg = ADC_WINCTRL_WINMODE_DISABLE;	// Disable window monitor mode
	while(ADC->STATUS.bit.SYNCBUSY);

	ADC->EVCTRL.reg |= ADC_EVCTRL_STARTEI;	//start ADC when event occurs
	while(ADC->STATUS.bit.SYNCBUSY);

	ADC->CTRLA.reg |= ADC_CTRLA_ENABLE;	//set ADC to run in standby
	while(ADC->STATUS.bit.SYNCBUSY);
}

//This function sets up an ADC interrupt that is triggered 
//when an ADC value is out of range of the window
//input argument is priority of interrupt (0 is highest priority)
void interruptSetup(byte priority)
{

	ADC->INTENSET.reg |= ADC_INTENSET_RESRDY;	// enable ADC ready interrupt
	while(ADC->STATUS.bit.SYNCBUSY);

	NVIC_EnableIRQ(ADC_IRQn);			// enable ADC interrupts
	NVIC_SetPriority(ADC_IRQn, priority);	//set priority of the interrupt
}

//software trigger to start ADC in free run
//in future could use this to set various ADC triggers
void adcSWTrigger()
{
	ADC->SWTRIG.reg |= ADC_SWTRIG_START;
}
