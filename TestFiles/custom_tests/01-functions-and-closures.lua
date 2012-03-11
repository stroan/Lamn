-------------------------------------------
-- Simple function calling test

function test1(c)
  return 1 + c, 2, c + 3
end

t1, t2, t3 = test1(test1(4))
assert(t1 == 6)
assert(t2 == 2)
assert(t3 == 8)

t1 = test1(test1(4)), 1
assert(t1 == 6)

-------------------------------------------
-- Simple closure test

local test2a = 1

function test2(c)
  return test2a + c
end

assert(test2(2) == 3)

test2a = 2

assert(test2(2) == 4)

-------------------------------------------
-- Simple closure test2

local test3b = "dummy"
local test3c = "dummy"
local test3a = 1
local test3d = "dummy"

function test3()
  test3a = test3a + 1
  return test3a
end

assert(test3() == 2)
assert(test3() == 3)
assert(test3() == 4)

-------------------------------------------
-- Local function test

local test4a = "dummy"

local function test4(a)
  local function test4b()
    return "hello world", a
  end
  return test4b
end

t1, t2 = test4(5)()
assert(t1 == "hello world")
assert(t2 == 5)