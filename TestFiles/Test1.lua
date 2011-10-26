function foo(c)
  return 1 + c, 2, c + 3
end

print("Should output:\t6\t2\t8")
print("Actual output:", foo(foo(4)))

print("===============================")

print("Should output:\t6\t1")
print("Actual output:", foo(foo(4)), 1)   -- Should output 6 1