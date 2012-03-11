Vector = {x = 0, y = 0}
function Vector.new()
  local o = {}
  setmetatable(o, Vector)
  return o
end

function Vector:add(b)
  self.x = self.x + b.x
  self.y = self.y + b.y
end

function Vector:__add(b)
  local r = Vector.new()
  r.x = self.x + b.x
  r.y = self.y + b.y
  return r
end

v1 = Vector.new()
v2 = Vector.new()

assert(v1.x == 0)
assert(v1.y == 0)
assert(v2.x == 0)
assert(v2.y == 0)

v1.x = 2
assert(v1.x == 2)
v2.y = 3
assert(v2.y == 3)

v1:add(v2)
assert(v1.x == 2)
assert(v1.y == 3)

assert((v1 + v2).x == 2)
assert((v1 + v2).y == 6)