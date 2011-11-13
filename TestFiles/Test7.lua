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

print("v1", v1)
print("v2", v2)

print("v1.x", v1.x)
v1.x = 2
print ("v1", v1)
v2.y = 3
print ("v2", v2)

v1:add(v2)
print("v1", v1)
print("v1.x", v1.x)
print("v1 + v2", v1 + v2)