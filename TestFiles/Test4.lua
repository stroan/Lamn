function iterator(max, current)
  if current == max then
    return nil
  end
  return current + 1
end

for i in iterator, 10, 0 do
  print(1,i)
end

for i = 1,10 do
  print(2,i)
end