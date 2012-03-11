t = {az = 1}
assert(t.az == 1)

t = {az = 2 + 3, b = (function () return 1,2,3 end)}
assert(t.b() == 1)

t = {1,2,3,["sdf"] = 3}
assert(t[1] == 1)
assert(t[3] == 3)
assert(t["sdf"] == 3)

function foo(...)
  return {az = "sf","foo",...}
end

t = foo(1,2,3)
assert(t[1] == "foo")
assert(t[4] == 3)

t = {}
t[1] = 42
t.b = 43
assert(t[1] == 42)
assert(t.b == 43)

Vector = {a= 2, b = {}}
function Vector.b:foo()
  return 1
end

assert(Vector.b:foo() == 1)