function test1()
  local a = 1
  while not (a == 10) do
    print("oops",a)
	a = a + 1
  end
end

test1()