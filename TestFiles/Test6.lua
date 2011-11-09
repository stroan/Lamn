print {a = 1}
print {a = 2 + 3, b = (function () return 1,2,3 end)}
print {1,2,3,["sdf"] = 3}

function foo(...)
  print {a = "sf","foo",...}
end

foo(1,2,3)

local a = {}
print(a)