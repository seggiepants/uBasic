DIM A[3]

INPUT "Three things: "; A[0], A[1], A[2]
PRINT "The things: "; A[0]; ", "; A[1]; ", "; A[2]

RESTORE letters
READ A[1 + 1], n2, n3, n4, n5
PRINT "Letters? "
GOSUB printMe

RESTORE 100
READ A[2], n2, n3, n4, n5
PRINT "Numbers? "
GOSUB printMe

RESTORE
READ A[2], n2, n3, n4, n5
PRINT "Words? "
GOSUB printMe

END

printMe:
PRINT A[2]; ", " ; n2 ; ", "; n3; ", "; n4; ", "; n5
RETURN

DATA "I", "LIKE", "TO", "EAT", "POTATOES"
100 DATA 1, 2, 3, 4, 5
letters:
DATA "A", "B", "C", "D", "E"
