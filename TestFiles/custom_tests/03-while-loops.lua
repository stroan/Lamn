function test1()
    local a = 1
    local c = 1
    while not (a == 10) do
    a = a + 1
    if (a == 4) then
      local b = 1
      while not (b == 4) do
        b = b + 1
        c = c + 1
      end
      break
	end
  end
  return a, c
end

t1, t2 = test1()
assert(t1 == 4)
assert(t2 == 4)

function fibs(c)
  local a, b, i = 1, 1, 0
  while not (i == c) do
    a, b = b, a + b
    i = i + 1
  end
  return b
end

assert(fibs(20) == 17711)

ri = 1
repeat
  ri = ri + 1
until ri == 3

assert(ri == 3)