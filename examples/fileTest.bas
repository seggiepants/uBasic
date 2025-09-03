num = FREEFILE
fileName = "test.txt"

OPEN fileName FOR OUTPUT AS num
PRINT #num, "Hello, World"
CLOSE num

OPEN fileName FOR INPUT as num
LOOP1:
INPUT #num, message
PRINT message
IF NOT EOF(num) THEN GOTO LOOP1
CLOSE num

OPEN fileName FOR APPEND AS num
PRINT #num, "Extra Line"
CLOSE num

OPEN fileName FOR INPUT as num
LOOP2:
INPUT #num, message
PRINT message
IF NOT EOF(num) THEN GOTO LOOP2
CLOSE num

OPEN fileName FOR OUTPUT AS num
PRINT #num, "Hello World, 42, 3.14159, True"
CLOSE num

OPEN fileName FOR INPUT as num
INPUT #num, message, numInt, numFloat, boolVal
PRINT message;", ";numInt;", ";numFloat;", ";boolVal
CLOSE num
