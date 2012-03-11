function iterator(max, current)
  if current == max then
    return nil
  end
  return current + 1
end

a = 0
for i in iterator, 10, 0 do
  a = a + i
end

assert(a == 55)

a = 0
for i = 1,10 do
  a = a + i
end

assert(a == 55)