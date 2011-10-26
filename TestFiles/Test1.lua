function foo(c)
  return 1 + c, 2, c + 3
end

print(foo(foo(4)))
print(foo(foo(4)), 1)