assert((false or 3) == 3)
assert((false and 4) == false)
assert((nil and 5) == nil)
assert((true and 6) == 6)
assert((4 and 7) == 7)

local a = function () return 1,2,3 end
t1, t2, t3 = a()
assert(t1 == 1)
assert(t2 == 2)
assert(t3 == 3)

local b = function (...) return ... end
t1, t2, t3 = b(1,2,3)
assert(t1 == 1)
assert(t2 == 2)
assert(t3 == 3)