LET BOTTLES = 99: LET BOTTLES$ = "99": LET BOTTLE$ = " bottles"
FOR A = 1 TO 99
PRINT BOTTLES$; BOTTLE$; " of beer on the wall, "; BOTTLES$; BOTTLE$; " of beer."
LET BOTTLES = BOTTLES - 1
IF BOTTLES > 0 THEN LET BOTTLES$ = LTRIM$(STR$(BOTTLES)): LET PRONOUN$ = "one"
IF BOTTLES == 0 THEN LET BOTTLES$ = "no more": LET PRONOUN$ = "it"
IF BOTTLES <> 1 THEN LET BOTTLE$ = " bottles"
IF BOTTLES == 1 THEN LET BOTTLE$ = " bottle"
PRINT "Take "; PRONOUN$; " down and pass it around, "; BOTTLES$; BOTTLE$; " of beer on the wall."
PRINT : NEXT A
PRINT "No more bottles of beer on the wall, no more bottles of beer."
PRINT "Go to the store and buy some more, 99 bottles of beer on the wall."