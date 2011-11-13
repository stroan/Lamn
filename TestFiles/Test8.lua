co = coroutine.create(function (a)
  print(a)
  local b = coroutine.yield(1)
  print(b)
end)

print(coroutine.resume(co, "foo"))
print(coroutine.resume(co, "bar"))
print("done")