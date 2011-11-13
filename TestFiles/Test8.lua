co = coroutine.create(function (a)
  print(a)
  local b = coroutine.yield(1)
  print(b)
  return 1,2,3
end)

print(coroutine.resume(co, "foo"))
print(coroutine.resume(co, "bar"))
print(coroutine.resume(co, "baz"))
print("done")