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