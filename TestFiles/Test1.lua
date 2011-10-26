function foo(c)
  return 1 + c, 2, c + 3
end

print(foo(foo(4)))