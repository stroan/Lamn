local a = function () print(1,2,3) end
a()

print(false or 3)
print(false and 4)
print(nil and 5)
print(true and 6)
print(4 and 7)

local b = function (...) print(...) end
b(1,2,3)