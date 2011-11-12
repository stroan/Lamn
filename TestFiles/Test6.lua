--[[print {az = 1}
print {az = 2 + 3, b = (function () return 1,2,3 end)}
print {1,2,3,["sdf"] = 3}

function foo(...)
  print {az = "sf","foo",...}
end

foo(1,2,3)

local a = {}
print(a)

a[1] = 42
print(a)

a.b = 43
print(a)

print(a.b)
print(a["b"])]]

Vector = {a= 2, b = {}}
function Vector.b:foo()
  print("hello", self)
  return 1
end

Vector.b:foo()