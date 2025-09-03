dim x[10]
dim xy[10, 10]

for i = 0 to 9
	x[i] = i
next

for j = 0 to 9
	for i = 0 to 9
		xy[i, j] = (i + 1) * (j + 1)
	next
next
for i = 0 to 9
	print i;": ";x[i]
next i

for j = 0 to 9
	for i = 0 to 9
		if i > 0 then
			print ", ";
		end if
		print tab(i * 4);xy[i, j];		
	next
	print
next

			