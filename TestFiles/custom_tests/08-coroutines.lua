co = coroutine.create(function (a)
  local b = coroutine.yield(1)
  return 1,2,3
end)

t1, t2 = coroutine.resume(co, "foo")
assert(t1)
assert(t2 == 1)

t1, t2, t3, t4 = coroutine.resume(co, "bar")
assert(t1)
assert(t2 == 1)
assert(t3 == 2)
assert(t4 == 3)

assert(coroutine.resume(co, "baz") == false)