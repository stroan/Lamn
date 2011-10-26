-------------------------------------------
-- Simple function calling test

print("Simple function calling test")

function test1(c)
  return 1 + c, 2, c + 3
end

print("Should output:\t6\t2\t8")
print("Actual output:", test1(test1(4)))

print("Should output:\t6\t1")
print("Actual output:", test1(test1(4)), 1)

print("===============================")

-------------------------------------------
-- Simple closure test

print("Simple closure test")

local test2a = 1

function test2(c)
  return test2a + c
end

print("Should output:\t3")
print("Actual output:", test2(2))

test2a = 2

print("Should output:\t4")
print("Actual output:", test2(2))

print("===============================")

-------------------------------------------
-- Simple closure test2

print("Simple closure test2")

local test3b = "dummy"
local test3c = "dummy"
local test3a = 1
local test3d = "dummy"

function test3()
  test3a = test3a + 1
  return test3a
end

print("Should output:\t2\t3\t4")
print("Actual output:", test3(), test3(), test3())

print("===============================")