# BMPARSE - SE.BMS Editor
BMPARSE is an editing tool for the SE.BMS in Pikmin 2.

It works by assembling a series of text files and linking them into a .bms file for the game to use. 

You can edit these text files to make changes to sounds ingame, or to discover what instruments particular sounds are using (which you can then trace back to the relevant IBNK and WSYS). 

Generally this requires hex editing, but now you can edit it as text. 

**BMPARSE does not produce a 1:1 se.bms** 

# Files 

### project.json
This is the project container. It contains the metadata for the BMS. If you want to add an include or a sound , it will be done in here. 

**NOTE**: as BMPARSE handles the category compilation for you automatically, if you put a sound in the middle of an array, it will shift all of the sound ID's. If you're adding a new sound to a category, always add it to the end of the array. 

### init.txt 
The initializer code for the BMS file. You should never really have to touch this unless you want to modify how sound categories are used globally.

### cat
The category initializer code for each sound category, modifying how each sound in each category is played back. You can push flags or effects for sounds here

**NOTE**: This type of include contains a special instruction called "%CATEGORY_CALLTABLE". Removing or relocating this instruction will break everything. The data for the sound list gets put here at compilation time. 

**NOTE**: Pikmin2's categories are out of order. The following category files correspond to their ID's

| Category ID | File Name |
| --- | ----------- |
| 0 | CATEGORY_2 |
| 1 | CATEGORY_3 |
| 2 | CATEGORY_4 |
| 3 | CATEGORY_5 |
| 4 | CATEGORY_6 |
| 5 | CATEGORY_1 |

### common
Common is a folder that contains deduplicated code. If the same code is referenced more than two times, it was made a COMMON call during disasembly. 

### sounds
This contains folders, which contain the individual text files for each sound. This is where you will do most of the editing 

# BMSLanguage 
BMSLanguage is an assembly-like language designed to mimic functionality of the BMS bytecode. BMPARSE compiles this into BMS code and links it. 
There are hundreds of instructions in BMPARSE

## Symbolism and language 

### Labels 

There are two primary types of lables: Local and Global.

Any type of label is defined with the : character, for example you may define a label like this

```
:AGAIN
... code
... code
JMP 0h AGAIN
```

Local labels can be accessed within the same file only, and are shown as above. 

Global labels can be accessed across any files, and must have a globally unique name.

A global label is defined by putting an '@' character in front of a label name 

```
:@AGAIN
...code
...code
...code
JMP 0h @AGAIN
```

Notice how the JMP instruction must also explicitly reference the global label. If you make a label global, you must update all references to it. 

### Numbers and Data Specifiers

#### Numbers
Numbers in BMSLang have one of two formats: Decimal and Hexadecimal

Numbers are explicitly decimal or hexadecimal.

Normal decimal numbers will look like "123456" suffixed with nothing.

Hexadecimal numbers will be suffixed with an 'h' character.

#### Data Specifiers

A primary data specifier in BMSLang is the HEX() operator. The HEX operator is a result of the disassembler being unable to capture particular types of polymorphism in instructions. 

HEX() cannot be used indepdendently, but it can be used as an argument to instructions which accept it as an argument. 

**All numbers within the HEX function are parsed as hexadecimal numbers** 

Generally, arguments of the HEX function end up being written as raw data to the file. 

Numbers in the HEX function are separated by the comma ',' character.

You may see instructions like the following as examples.

``NOTEONEXT 1Eh 2h EFh DAh HEX(2,DA)``


### Style

Generated code is usually CAPITALIZED

Labels are CAPITALIZED 

User generated code is lowercase


### Instructions (unfinished)

| opcode                             | name                                | description                                                                                                        | 
| ---------------------------------- | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| ALIGN4                             | Alignment 4                         | Pads the sequence data to 4 bytes (required before a jumptable or data target)                                     | 
| CHPORTI [n]8                       | Check Port Import                   | Checks the port [n] on the current track for waiting data. Writes 1 to r3 if waiting                               |
| INTTIME [n]8                       | Set Interrupt Timer                 | Sets a timeout for the specified interrupt                                                                         | 
| OPENTRACK [n] [label]              | Open Track                          | Opens the track with the id [n] at [label]                                                                         | 
| PANSWEEP [n1]8 [n2]8 [n3]8         | Sets pan sweep, left, center, right |                                                                                                                    | 
| PARAM16 [n1]8 [n2]s16              | Set 16 bit register?                | Set 16 bit register [n1] to [n2]                                                                                   | 
| PARAM16LB [n1]8 [label]            | 16 bit register label reference     | Sets reguster [n1] to reference the address of [label]                                                             |
| PARAM8 [n1]8 [n2]8                 | Set 8 bit register                  | Sets register [n1] to value [n2]                                                                                   |
| REF24 [label]                      | Reference 24-bit                    | Writes the 24 bit address specified at [label] directly to the sequenced. Used for building calltables.            |
| RETIMM                             | Return Immediately                  | Returns without checking a condition                                                                               |
| SET_BANK_INS [n1]8 [n2]8           | Set bank and instrument             | Alias for setting the bank and instrument of a sequence                                                            |
| SETINTER [n]8 [label]              | Set Interrupt                       | Sets the interrupt ID [n] to jump to the address [label] when fired.                                               |
| SETPARAM90 [n1]8 [n2]8             | Set 8 bit register again?           | Sets register [n1] to value [n2]                                                                                   |
| SETPARAM91 [n1]8 [n2]16            | Sets 16 bit register                | Sets register [n1] to value [n2]                                                                                   |
| SETPARAM92 [n1] [n2]               | Set 16 bit register?                | Set 16 bit register [n1] to [n2]                                                                                   |
| SETPARAM93 [n1]8 [n2]16            | Sets 16 bit register?               | Sets register [n1] to value [n2]                                                                                   |
| SIMPLEENV [n]8 [label]             | Simple Envelope                     | Creates an envelope [n] using the data at [label] (Points to the 'envpoint' instruction, terminated by a STOPcode) |
| TPRMS16 [n1]8 [n2]s16              | Sets parameter [n1] to [n2]         |                                                                                                                    |
| TPRMS16_DU8_9E [n1]8 [n2]s16 [n3]8 | Parameter slide u8 with u8 duration | Slides parameter [n1] to value [n2] over [n3] ticks.                                                               |
| TPRMU8_DU8 [n1]8 [n2]8 [n3]8       | Parameter slide u8 with u8 duration | Slides parameter [n1] to value [n2] over [n3] ticks.                                                               |
| TREL [n]8                          | Timerelate                          | Performs time relation between tracks                                                                              |
| TRELJV0 [n]8 [HEX]                 | Timerelate for older JAudio         |                                                                                                                    |
| UPSYNC [n]8                        | Update Sync                         | ???                                                                                                                |
| VIBDEPTHMIDI [n1]8 [n2]8           | Vibrato Depth Midi                  | Sets vibrato depth (coarse, fine)                                                                                  |
| VIBPITCH [n]8                      | Vibrato Pitch                       | Sets vibrato pitch [n] semitones                                                                                   |

# Important Trivia

## Structure of a sound ID
A sound ID is generally a u16, meaning 4 hexadecimal characters

![image](https://xayr.gay/share/05-2026/ebaaf18e-557d-44b7-a904-564a773b0a09.png) 

The first bits are generally the category. 

Categories can go from 0-16 
Runtime flags are generally 8
The rest of the bits are an ID. leaving a theoretical maximum of 131,072 sounds per category.

This means a soundID in each category might look like 
0x2801 

This translates to Category 2, Play Once, Sound 1. 

Sounds in a category are sequential. Meaning if the last soundid in a category is 36, and you add another sound after it, the next will be 37...38...39... etc. 
