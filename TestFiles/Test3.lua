function test1()
  local a = 1
  while not (a == 10) do
    print("oops",a)
	a = a + 1
	if (a == 4) then
	  local b = 1
	  while not (b == 4) do
	    print("blap", a, b)
		b = b + 1
	  end
	  break
	end
  end
end

test1()

function fibs(c)
  local a, b, i = 1, 1, 0
  print(a)
  print(b)
  while not (i == c) do
    a, b = b, a + b
	print(b)
    i = i + 1
  end
end

fibs(20)

do
  print "hello world"
end

ri = 1
repeat
  print "hello world"
  ri = ri + 1
until ri == 3